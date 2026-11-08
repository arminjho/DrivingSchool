using eAutoskola.Application.Notifications;
using eAutoskola.Application.Payments;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Messaging;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Application.Lessons;

/// <summary>
/// The command half of the Lesson CQRS split - every state transition goes through
/// LessonStateMachine.EnsureCanTransition first, then: (1) ONE atomic SaveChangesAsync for the
/// Lesson, its LessonStatusHistory and the staged Notification(s) (and, where relevant, the
/// extra-lesson charge or the refund), (2) the SignalR push of those notifications and (3) the
/// RabbitMQ publish for the e-mail. Steps 2-3 run only after the commit and never fail the
/// request (both swallow and log their own errors).
/// </summary>
public class LessonCommandService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationService _notificationService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly LessonPaymentService _lessonPayments;
    private readonly ILogger<LessonCommandService> _logger;

    public LessonCommandService(
        EAutoskolaDbContext context,
        ICurrentUserService currentUser,
        NotificationService notificationService,
        IMessagePublisher messagePublisher,
        LessonPaymentService lessonPayments,
        ILogger<LessonCommandService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _messagePublisher = messagePublisher;
        _lessonPayments = lessonPayments;
        _logger = logger;
    }

    public async Task<LessonDto> BookAsync(LessonBookRequest request, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;
        var endsAtUtc = request.StartsAtUtc + LessonSlotPolicy.SlotDuration;

        var student = await _context.Users.Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), studentId);

        if (!student.IsActive)
        {
            throw new BusinessException(Messages.StudentAccountDeactivated);
        }
        if (student.StudentProfile is null)
        {
            throw new BusinessException(Messages.StudentProfileNotFound);
        }

        var vehicleId = await ValidateSlotAsync(
            request.InstructorId, studentId, student.StudentProfile.VehicleCategoryId,
            request.StartsAtUtc, endsAtUtc, request.DrivingZoneId, null, cancellationToken);
        var lessonNumber = await _context.Lessons.CountAsync(l => l.StudentId == studentId, cancellationToken) + 1;

        var lesson = new Lesson
        {
            StudentId = studentId,
            InstructorId = request.InstructorId,
            VehicleId = vehicleId,
            DrivingZoneId = request.DrivingZoneId,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = endsAtUtc,
            LessonNumber = lessonNumber,
            Note = request.Note,
            Status = LessonStatus.Pending,
            RequestStatusId = await RequestStatusIdAsync(LessonStatus.Pending, cancellationToken),
        };

        _context.Lessons.Add(lesson);
        _context.LessonStatusHistories.Add(new LessonStatusHistory
        {
            Lesson = lesson,
            FromStatus = LessonStatus.Pending,
            ToStatus = LessonStatus.Pending,
            ChangedByUserId = studentId,
            Reason = Messages.HistoryLessonRequested,
        });
        _notificationService.Stage(
            request.InstructorId, Messages.NotificationLessonRequestedTitle,
            string.Format(Messages.NotificationLessonRequestedBody, $"{student.FirstName} {student.LastName}"),
            NotificationType.LessonCreated);
        await _notificationService.StageForAdminsAsync(
            Messages.NotificationLessonRequestedTitle,
            string.Format(Messages.NotificationLessonRequestedBody, $"{student.FirstName} {student.LastName}"),
            NotificationType.LessonCreated, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.PushStagedAsync(cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> RescheduleAsync(int id, LessonRescheduleRequest request, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);

        if (lesson.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
        if (lesson.Status != LessonStatus.Pending)
        {
            throw new BusinessException(Messages.LessonRescheduleOnlyPending);
        }

        var endsAtUtc = request.StartsAtUtc + LessonSlotPolicy.SlotDuration;
        var student = await _context.StudentProfiles.FirstAsync(p => p.UserId == lesson.StudentId, cancellationToken);

        lesson.VehicleId = await ValidateSlotAsync(
            lesson.InstructorId, lesson.StudentId, student.VehicleCategoryId,
            request.StartsAtUtc, endsAtUtc, request.DrivingZoneId, lesson.Id, cancellationToken);
        lesson.StartsAtUtc = request.StartsAtUtc;
        lesson.EndsAtUtc = endsAtUtc;
        lesson.DrivingZoneId = request.DrivingZoneId;

        // Same pattern as BookAsync: staged on this request's shared context, so the reschedule
        // and the instructor's notification commit or fail together in TransitionAsync's save.
        _notificationService.Stage(
            lesson.InstructorId, Messages.NotificationLessonRescheduledTitle,
            string.Format(Messages.NotificationLessonRescheduledBody, lesson.StartsAtUtc),
            NotificationType.LessonRescheduled);

        _context.LessonStatusHistories.Add(new LessonStatusHistory
        {
            LessonId = lesson.Id, FromStatus = lesson.Status, ToStatus = lesson.Status,
            ChangedByUserId = _currentUser.UserId, Reason = Messages.HistoryLessonRescheduled,
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.PushStagedAsync(cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        EnsureInstructorOwnerOrAdmin(lesson);
        LessonStateMachine.EnsureCanTransition(lesson.Status, LessonStatus.Confirmed);

        if (lesson.StartsAtUtc <= DateTime.UtcNow)
        {
            throw new BusinessException(Messages.LessonSlotAlreadyPassed);
        }
        if (lesson.VehicleId is null)
        {
            throw new BusinessException(Messages.LessonConfirmNeedsVehicle);
        }

        // The vehicle assigned at booking time may have been sent for service or removed from
        // the fleet since - re-check it's still usable right before confirming, and reassign a
        // free one of the same category rather than confirming with a vehicle that no longer is.
        var vehicleStillActive = await _context.Vehicles
            .AnyAsync(v => v.Id == lesson.VehicleId && v.Status == VehicleStatus.Active, cancellationToken);
        if (!vehicleStillActive)
        {
            var studentCategoryId = await _context.StudentProfiles
                .Where(p => p.UserId == lesson.StudentId)
                .Select(p => p.VehicleCategoryId)
                .FirstAsync(cancellationToken);
            var replacementVehicleId = await FindAvailableVehicleAsync(
                studentCategoryId, lesson.StartsAtUtc, lesson.EndsAtUtc, lesson.Id, cancellationToken);
            lesson.VehicleId = replacementVehicleId ?? throw new BusinessException(Messages.LessonConfirmVehicleUnavailable);
        }

        // Staged on the context here, committed by TransitionAsync's single SaveChangesAsync -
        // the extra-lesson charge, the notification and the confirmation succeed or fail together.
        var extraLessonPrice = await _lessonPayments.AddExtraLessonChargeIfNeededAsync(lesson, cancellationToken);

        var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        _notificationService.Stage(lesson.StudentId, Messages.NotificationLessonConfirmedTitle,
            extraLessonPrice is { } price
                ? string.Format(Messages.NotificationLessonConfirmedExtraBody, price)
                : Messages.NotificationLessonConfirmedBody,
            NotificationType.LessonConfirmed);

        await TransitionAsync(lesson, LessonStatus.Confirmed, Messages.HistoryLessonConfirmed, cancellationToken);
        await _notificationService.PushStagedAsync(cancellationToken);

        await _messagePublisher.PublishAsync(new LessonConfirmedMessage
        {
            LessonId = lesson.Id, ToEmail = student.Email, ToName = $"{student.FirstName} {student.LastName}", StartsAtUtc = lesson.StartsAtUtc,
        }, cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> RejectAsync(int id, LessonReasonRequest request, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        EnsureInstructorOwnerOrAdmin(lesson);
        LessonStateMachine.EnsureCanTransition(lesson.Status, LessonStatus.Cancelled);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException(nameof(request.Reason), Messages.RequiredField);
        }

        var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        _notificationService.Stage(lesson.StudentId, Messages.NotificationLessonRejectedTitle,
            string.Format(Messages.NotificationLessonRejectedBody, request.Reason), NotificationType.LessonRejected);

        await TransitionAsync(lesson, LessonStatus.Cancelled, request.Reason, cancellationToken, request.Reason);
        await _notificationService.PushStagedAsync(cancellationToken);

        await _messagePublisher.PublishAsync(new LessonRejectedMessage
        {
            LessonId = lesson.Id, ToEmail = student.Email, ToName = $"{student.FirstName} {student.LastName}", Reason = request.Reason,
        }, cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> CancelAsync(int id, LessonReasonRequest? request, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        var isStudent = lesson.StudentId == _currentUser.UserId;
        var isInstructor = lesson.InstructorId == _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole(Role.Admin);

        if (!isStudent && !isInstructor && !isAdmin)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }

        LessonStateMachine.EnsureCanTransition(lesson.Status, LessonStatus.Cancelled);

        var reason = request?.Reason;
        // The student may cancel their own still-Pending request without a reason; anyone else
        // (instructor or admin) must always explain why, Pending or Confirmed - otherwise an
        // instructor's /cancel on a Pending request bypassed the reason /reject already requires.
        var requiresReason = !isStudent || lesson.Status == LessonStatus.Confirmed;
        if (requiresReason && string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", Messages.RequiredField);
        }

        // Only the lesson's own Student is limited by the cutoff; Instructors and Admins
        // always may cancel (and refund). Runs before the generic cutoff below so a paid
        // lesson gets the refund-specific message.
        var actorIsStudent = isStudent && !isAdmin;
        var paymentPlan = await _lessonPayments.PrepareCancellationAsync(lesson, actorIsStudent, cancellationToken);

        if (lesson.Status == LessonStatus.Confirmed && actorIsStudent
            && lesson.StartsAtUtc - DateTime.UtcNow < TimeSpan.FromHours(LessonStateMachine.CancellationCutoffHours))
        {
            throw new BusinessException(string.Format(Messages.LessonCancelCutoff, LessonStateMachine.CancellationCutoffHours));
        }

        // The PayPal refund happens here, before any lesson/charge state is touched: if it
        // fails nothing changes and the exception aborts the cancellation.
        var refund = await _lessonPayments.ApplyCancellationAsync(paymentPlan, lesson, cancellationToken);

        var notifyUserId = isStudent ? lesson.InstructorId : lesson.StudentId;
        var notifyUser = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == notifyUserId, cancellationToken);
        _notificationService.Stage(notifyUserId, Messages.NotificationLessonCancelledTitle,
            reason is null
                ? Messages.NotificationLessonCancelledBody
                : string.Format(Messages.NotificationLessonCancelledWithReasonBody, reason),
            NotificationType.LessonCancelled);

        var lessonStudent = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        var studentName = $"{lessonStudent.FirstName} {lessonStudent.LastName}";
        await _notificationService.StageForAdminsAsync(
            Messages.NotificationLessonCancelledTitle,
            string.Format(Messages.NotificationAdminLessonCancelledBody, studentName),
            NotificationType.LessonCancelled, cancellationToken);

        User? refundedStudent = null;
        if (refund is not null)
        {
            refundedStudent = lessonStudent;
            _notificationService.Stage(lesson.StudentId, Messages.NotificationRefundTitle,
                string.Format(Messages.NotificationRefundBody, refund.AmountKm), NotificationType.RefundIssued);
            await _notificationService.StageForAdminsAsync(
                Messages.NotificationRefundTitle,
                string.Format(Messages.NotificationAdminRefundBody, studentName, refund.AmountKm),
                NotificationType.RefundIssued, cancellationToken);
        }

        try
        {
            await TransitionAsync(lesson, LessonStatus.Cancelled, reason ?? Messages.HistoryLessonCancelled, cancellationToken, reason);
        }
        catch (Exception ex) when (refund is not null)
        {
            // The buyer has been refunded but the database write failed - needs manual reconciliation.
            _logger.LogCritical(ex,
                "RECONCILE: PayPal refund succeeded but saving the cancellation failed. Lesson {LessonId}, payment {PaymentId}, capture {CaptureId}, amount {AmountKm} KM",
                lesson.Id, paymentPlan?.CompletedPayment?.Id, paymentPlan?.CompletedPayment?.PayPalCaptureId, refund.AmountKm);
            _notificationService.DiscardStaged();
            throw;
        }

        await _notificationService.PushStagedAsync(cancellationToken);

        if (refund is not null && refundedStudent is not null)
        {
            await _messagePublisher.PublishAsync(new RefundIssuedMessage
            {
                ToEmail = refundedStudent.Email, ToName = $"{refundedStudent.FirstName} {refundedStudent.LastName}", Amount = refund.AmountKm,
            }, cancellationToken);
        }

        await _messagePublisher.PublishAsync(new LessonCancelledMessage
        {
            LessonId = lesson.Id, ToEmail = notifyUser.Email, ToName = $"{notifyUser.FirstName} {notifyUser.LastName}", Reason = reason ?? string.Empty,
        }, cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> StartAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        EnsureInstructorOwnerOrAdmin(lesson);

        if (lesson.Status != LessonStatus.Confirmed)
        {
            throw new BusinessException(Messages.LessonStartOnlyConfirmed);
        }

        var earliestStart = lesson.StartsAtUtc - TimeSpan.FromMinutes(LessonStateMachine.StartToleranceMinutes);
        if (DateTime.UtcNow < earliestStart)
        {
            throw new BusinessException(string.Format(Messages.LessonStartTooEarly, earliestStart, LessonStateMachine.StartToleranceMinutes));
        }

        lesson.StartedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> CompleteAsync(int id, LessonCompleteRequest request, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        EnsureInstructorOwnerOrAdmin(lesson);
        LessonStateMachine.EnsureCanTransition(lesson.Status, LessonStatus.Completed);

        if (lesson.StartedAt is null)
        {
            throw new BusinessException(Messages.LessonCompleteNeedsStart);
        }
        if (DateTime.UtcNow < lesson.StartsAtUtc)
        {
            throw new BusinessException(string.Format(Messages.LessonCompleteTooEarly, lesson.StartsAtUtc));
        }
        if (request.Rating is < 1 or > 5)
        {
            throw new ValidationException(nameof(request.Rating), Messages.RatingOutOfRange);
        }
        if (string.IsNullOrWhiteSpace(request.InstructorNotes))
        {
            throw new ValidationException(nameof(request.InstructorNotes), Messages.RequiredField);
        }

        lesson.Rating = request.Rating;
        lesson.InstructorNotes = request.InstructorNotes;
        lesson.CompletedAt = DateTime.UtcNow;

        var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        _notificationService.Stage(lesson.StudentId, Messages.NotificationLessonCompletedTitle,
            Messages.NotificationLessonCompletedBody, NotificationType.LessonCompleted);

        await TransitionAsync(lesson, LessonStatus.Completed, Messages.HistoryLessonCompleted, cancellationToken);
        await _notificationService.PushStagedAsync(cancellationToken);

        await _messagePublisher.PublishAsync(new LessonCompletedMessage
        {
            LessonId = lesson.Id, ToEmail = student.Email, ToName = $"{student.FirstName} {student.LastName}", Rating = request.Rating,
        }, cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    // --- shared helpers ---

    private async Task<Lesson> LoadAsync(int id, CancellationToken cancellationToken)
        => await _context.Lessons.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Lesson), id);

    private async Task<LessonDto> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Student).Include(l => l.Instructor).Include(l => l.Vehicle).Include(l => l.DrivingZone)
            .AsNoTracking()
            .FirstAsync(l => l.Id == id, cancellationToken);

        var dto = lesson.Adapt<LessonDto>();
        await LessonPaymentStatus.ApplyAsync(_context, [dto], cancellationToken);
        return dto;
    }

    private void EnsureInstructorOwnerOrAdmin(Lesson lesson)
    {
        if (_currentUser.IsInRole(Role.Admin))
        {
            return;
        }
        if (lesson.InstructorId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
    }

    /// <summary>Applies the status change plus its audit row and commits everything staged on the
    /// context so far (notifications, charges, refunds) in one SaveChangesAsync.</summary>
    private async Task TransitionAsync(Lesson lesson, LessonStatus to, string historyReason, CancellationToken cancellationToken, string? rejectionReason = null)
    {
        var from = lesson.Status;
        lesson.Status = to;
        lesson.RequestStatusId = await RequestStatusIdAsync(to, cancellationToken);
        if (rejectionReason is not null)
        {
            lesson.RejectionReason = rejectionReason;
        }

        _context.LessonStatusHistories.Add(new LessonStatusHistory
        {
            LessonId = lesson.Id, FromStatus = from, ToStatus = to,
            ChangedByUserId = _currentUser.UserId, Reason = historyReason,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The RequestStatus row whose Code is the enum name - the database-level mirror of
    /// <see cref="Lesson.Status"/> (see <see cref="Lesson.RequestStatusId"/>).</summary>
    private async Task<int> RequestStatusIdAsync(LessonStatus status, CancellationToken cancellationToken)
    {
        var code = status.ToString();
        var id = await _context.RequestStatuses.Where(s => s.Code == code).Select(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        return id != 0 ? id : throw new BusinessException(Messages.RequestStatusMissing);
    }

    /// <summary>The one slot-validation method, called by both BookAsync and RescheduleAsync, so
    /// a slot can never pass one path's checks and fail the other's: the start time must be a
    /// full hour within working hours, the instructor's VehicleCategoryId must match the
    /// student's, the driving zone must exist, the two don't already have a conflicting lesson,
    /// and a vehicle of that category must actually be free for the slot - returned here so the
    /// booking is never left Pending without one (that used to only fail later, at Confirm).</summary>
    private async Task<int> ValidateSlotAsync(
        int instructorId, int studentId, int studentVehicleCategoryId,
        DateTime startsAtUtc, DateTime endsAtUtc, int drivingZoneId,
        int? excludingLessonId, CancellationToken cancellationToken)
    {
        ValidateFutureDate(startsAtUtc);
        ValidateFullHourWithinWorkingHours(startsAtUtc);

        var instructor = await _context.Users.Include(u => u.InstructorProfile)
            .FirstOrDefaultAsync(u => u.Id == instructorId && u.Role == Role.Instructor, cancellationToken);
        if (instructor is null || !instructor.IsActive || instructor.InstructorProfile is null)
        {
            throw new ValidationException(nameof(LessonBookRequest.InstructorId), Messages.InstructorUnavailable);
        }
        if (instructor.InstructorProfile.VehicleCategoryId != studentVehicleCategoryId)
        {
            throw new ValidationException(nameof(LessonBookRequest.InstructorId), Messages.InstructorCategoryMismatch);
        }

        var drivingZoneExists = await _context.DrivingZones.AnyAsync(z => z.Id == drivingZoneId, cancellationToken);
        if (!drivingZoneExists)
        {
            throw new ValidationException(nameof(LessonBookRequest.DrivingZoneId), Messages.DrivingZoneNotFound);
        }

        await EnsureNoOverlapAsync(instructorId, studentId, startsAtUtc, endsAtUtc, excludingLessonId, cancellationToken);

        var vehicleId = await FindAvailableVehicleAsync(studentVehicleCategoryId, startsAtUtc, endsAtUtc, excludingLessonId, cancellationToken);
        return vehicleId ?? throw new ValidationException(nameof(LessonBookRequest.StartsAtUtc), Messages.NoVehicleAvailableForSlot);
    }

    private static void ValidateFutureDate(DateTime startsAtUtc)
    {
        if (startsAtUtc <= DateTime.UtcNow)
        {
            throw new ValidationException(nameof(LessonBookRequest.StartsAtUtc), Messages.SlotMustBeInFuture);
        }
    }

    /// <summary>LessonSlotPolicy models every slot as a full hour between WorkDayStart and
    /// WorkDayEnd (e.g. 08:00, 09:00, ..., 17:00 for an 08:00-18:00 day) - anything off the hour,
    /// or starting too late for a full SlotDuration to fit before WorkDayEnd, is rejected here
    /// instead of silently being accepted and only looking wrong on a calendar later.</summary>
    private static void ValidateFullHourWithinWorkingHours(DateTime startsAtUtc)
    {
        var timeOfDay = startsAtUtc.TimeOfDay;
        var latestStart = LessonSlotPolicy.WorkDayEnd - LessonSlotPolicy.SlotDuration;

        if (timeOfDay.Seconds != 0 || timeOfDay.Milliseconds != 0 || timeOfDay.Minutes != 0
            || timeOfDay < LessonSlotPolicy.WorkDayStart || timeOfDay > latestStart)
        {
            throw new ValidationException(nameof(LessonBookRequest.StartsAtUtc), Messages.SlotOutsideWorkingHours);
        }
    }

    private async Task EnsureNoOverlapAsync(int instructorId, int studentId, DateTime startsAtUtc, DateTime endsAtUtc, int? excludingLessonId, CancellationToken cancellationToken)
    {
        var overlapping = await _context.Lessons.AnyAsync(l =>
            l.Id != (excludingLessonId ?? 0) &&
            (l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed) &&
            (l.InstructorId == instructorId || l.StudentId == studentId) &&
            l.StartsAtUtc < endsAtUtc && startsAtUtc < l.EndsAtUtc,
            cancellationToken);

        if (overlapping)
        {
            throw new ConflictException(Messages.SlotOverlaps);
        }
    }

    private async Task<int?> FindAvailableVehicleAsync(int vehicleCategoryId, DateTime startsAtUtc, DateTime endsAtUtc, int? excludingLessonId, CancellationToken cancellationToken)
    {
        var bookedVehicleIds = await _context.Lessons
            .Where(l => l.Id != (excludingLessonId ?? 0)
                && l.VehicleId != null
                && (l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed)
                && l.StartsAtUtc < endsAtUtc && startsAtUtc < l.EndsAtUtc)
            .Select(l => l.VehicleId!.Value)
            .ToListAsync(cancellationToken);

        return await _context.Vehicles
            .Where(v => v.VehicleCategoryId == vehicleCategoryId && v.Status == VehicleStatus.Active && !bookedVehicleIds.Contains(v.Id))
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
