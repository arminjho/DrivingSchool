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
/// LessonStateMachine.EnsureCanTransition first, then: (1) one atomic SaveChangesAsync for the
/// Lesson + LessonStatusHistory, (2) a best-effort Notification, (3) a best-effort RabbitMQ
/// publish. Steps 2-3 deliberately are NOT part of the same transaction as step 1 (same
/// treatment as every other side effect in this codebase).
/// </summary>
public class LessonCommandService
{
    private static readonly TimeSpan SlotDuration = TimeSpan.FromHours(1);

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
        var endsAtUtc = request.StartsAtUtc + SlotDuration;

        var student = await _context.Users.Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == studentId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), studentId);

        if (!student.IsActive)
        {
            throw new BusinessException("Vaš nalog je deaktiviran, nije moguće zakazati čas.");
        }
        if (student.StudentProfile is null)
        {
            throw new BusinessException("Profil učenika nije pronađen.");
        }

        await ValidateInstructorAsync(request.InstructorId, cancellationToken);
        ValidateFutureDate(request.StartsAtUtc);
        await EnsureNoOverlapAsync(request.InstructorId, studentId, request.StartsAtUtc, endsAtUtc, null, cancellationToken);

        var vehicleId = await FindAvailableVehicleAsync(student.StudentProfile.VehicleCategoryId, request.StartsAtUtc, endsAtUtc, null, cancellationToken);
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
        };

        _context.Lessons.Add(lesson);
        _context.LessonStatusHistories.Add(new LessonStatusHistory
        {
            Lesson = lesson,
            FromStatus = LessonStatus.Pending,
            ToStatus = LessonStatus.Pending,
            ChangedByUserId = studentId,
            Reason = "Kreiran zahtjev za čas.",
        });
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyAsync(
            request.InstructorId, "Novi zahtjev za čas",
            $"{student.FirstName} {student.LastName} je zatražio/la čas vožnje.",
            NotificationType.LessonCreated, cancellationToken);

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
            throw new BusinessException("Termin se može izmijeniti samo dok je zahtjev na čekanju.");
        }

        var endsAtUtc = request.StartsAtUtc + SlotDuration;
        ValidateFutureDate(request.StartsAtUtc);
        await EnsureNoOverlapAsync(lesson.InstructorId, lesson.StudentId, request.StartsAtUtc, endsAtUtc, lesson.Id, cancellationToken);

        var student = await _context.StudentProfiles.FirstAsync(p => p.UserId == lesson.StudentId, cancellationToken);
        lesson.VehicleId = await FindAvailableVehicleAsync(student.VehicleCategoryId, request.StartsAtUtc, endsAtUtc, lesson.Id, cancellationToken);
        lesson.StartsAtUtc = request.StartsAtUtc;
        lesson.EndsAtUtc = endsAtUtc;
        lesson.DrivingZoneId = request.DrivingZoneId;

        _context.LessonStatusHistories.Add(new LessonStatusHistory
        {
            LessonId = lesson.Id, FromStatus = lesson.Status, ToStatus = lesson.Status,
            ChangedByUserId = _currentUser.UserId, Reason = "Termin izmijenjen.",
        });
        await _context.SaveChangesAsync(cancellationToken);

        return await ReloadAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await LoadAsync(id, cancellationToken);
        EnsureInstructorOwnerOrAdmin(lesson);
        LessonStateMachine.EnsureCanTransition(lesson.Status, LessonStatus.Confirmed);

        if (lesson.VehicleId is null)
        {
            throw new BusinessException("Nije moguće potvrditi čas jer nije dodijeljeno vozilo.");
        }

        // Staged on the context here, committed by TransitionAsync's single SaveChangesAsync -
        // the extra-lesson charge and the confirmation succeed or fail together.
        var extraLessonPrice = await _lessonPayments.AddExtraLessonChargeIfNeededAsync(lesson, cancellationToken);

        await TransitionAsync(lesson, LessonStatus.Confirmed, "Zahtjev potvrđen.", cancellationToken);

        var student = await _context.Users.FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        await _notificationService.NotifyAsync(lesson.StudentId, "Čas potvrđen",
            extraLessonPrice is { } price
                ? $"Vaš zahtjev za čas vožnje je potvrđen. Ovaj čas nije obuhvaćen paketom i potrebno ga je platiti ({price:F2} KM)."
                : "Vaš zahtjev za čas vožnje je potvrđen.",
            NotificationType.LessonConfirmed, cancellationToken);
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

        await TransitionAsync(lesson, LessonStatus.Cancelled, request.Reason, cancellationToken, request.Reason);

        var student = await _context.Users.FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        await _notificationService.NotifyAsync(lesson.StudentId, "Zahtjev odbijen",
            $"Vaš zahtjev je odbijen: {request.Reason}", NotificationType.LessonRejected, cancellationToken);
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
        if (lesson.Status == LessonStatus.Confirmed && string.IsNullOrWhiteSpace(reason))
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
            throw new BusinessException($"Čas se može otkazati najkasnije {LessonStateMachine.CancellationCutoffHours}h prije termina.");
        }

        // The PayPal refund happens here, before any lesson/charge state is touched: if it
        // fails nothing changes and the exception aborts the cancellation.
        var refund = await _lessonPayments.ApplyCancellationAsync(paymentPlan, lesson, cancellationToken);

        try
        {
            await TransitionAsync(lesson, LessonStatus.Cancelled, reason ?? "Zahtjev otkazan.", cancellationToken, reason);
        }
        catch (Exception ex) when (refund is not null)
        {
            // The buyer has been refunded but the database write failed - needs manual reconciliation.
            _logger.LogCritical(ex,
                "RECONCILE: PayPal refund succeeded but saving the cancellation failed. Lesson {LessonId}, payment {PaymentId}, capture {CaptureId}, amount {AmountKm} KM",
                lesson.Id, paymentPlan?.CompletedPayment?.Id, paymentPlan?.CompletedPayment?.PayPalCaptureId, refund.AmountKm);
            throw;
        }

        if (refund is not null)
        {
            await NotifyRefundAsync(lesson, refund, cancellationToken);
        }

        var notifyUserId = isStudent ? lesson.InstructorId : lesson.StudentId;
        var notifyUser = await _context.Users.FirstAsync(u => u.Id == notifyUserId, cancellationToken);
        await _notificationService.NotifyAsync(notifyUserId, "Čas otkazan",
            reason is null ? "Čas vožnje je otkazan." : $"Čas vožnje je otkazan: {reason}",
            NotificationType.LessonCancelled, cancellationToken);
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
            throw new BusinessException("Čas se može započeti samo ako je potvrđen.");
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
            throw new BusinessException("Čas mora biti započet prije nego što se može završiti.");
        }
        if (request.Rating is < 1 or > 5)
        {
            throw new ValidationException(nameof(request.Rating), "Ocjena mora biti između 1 i 5.");
        }

        lesson.Rating = request.Rating;
        lesson.InstructorNotes = request.InstructorNotes;
        lesson.CompletedAt = DateTime.UtcNow;

        await TransitionAsync(lesson, LessonStatus.Completed, "Čas uspješno odrađen.", cancellationToken);

        var student = await _context.Users.FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
        await _notificationService.NotifyAsync(lesson.StudentId, "Čas završen",
            "Vaš čas vožnje je uspješno odrađen.", NotificationType.LessonCompleted, cancellationToken);
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

    // The refund is already done and committed, so a notification/e-mail problem must not turn
    // a successful cancellation into an error response.
    private async Task NotifyRefundAsync(Lesson lesson, RefundOutcome refund, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == lesson.StudentId, cancellationToken);
            await _notificationService.NotifyAsync(lesson.StudentId, "Povrat novca",
                $"Izvršen je povrat novca u iznosu od {refund.AmountKm:F2} KM.", NotificationType.RefundIssued, cancellationToken);
            await _messagePublisher.PublishAsync(new RefundIssuedMessage
            {
                ToEmail = student.Email, ToName = $"{student.FirstName} {student.LastName}", Amount = refund.AmountKm,
            }, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Refund for lesson {LessonId} was issued but its notification/e-mail could not be sent", lesson.Id);
        }
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

    private async Task TransitionAsync(Lesson lesson, LessonStatus to, string historyReason, CancellationToken cancellationToken, string? rejectionReason = null)
    {
        var from = lesson.Status;
        lesson.Status = to;
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

    private async Task ValidateInstructorAsync(int instructorId, CancellationToken cancellationToken)
    {
        var instructor = await _context.Users.FirstOrDefaultAsync(u => u.Id == instructorId && u.Role == Role.Instructor, cancellationToken);
        if (instructor is null || !instructor.IsActive)
        {
            throw new ValidationException(nameof(LessonBookRequest.InstructorId), "Odabrani instruktor nije dostupan.");
        }
    }

    private static void ValidateFutureDate(DateTime startsAtUtc)
    {
        if (startsAtUtc <= DateTime.UtcNow)
        {
            throw new ValidationException(nameof(LessonBookRequest.StartsAtUtc), "Termin mora biti u budućnosti.");
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
            throw new ConflictException("Odabrani termin se preklapa sa postojećim zahtjevom ili časom.");
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
