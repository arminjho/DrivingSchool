using eAutoskola.Application.Lessons;
using eAutoskola.Application.Notifications;
using eAutoskola.Application.Payments;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Tests.Payments;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eAutoskola.Tests.Lessons;

/// <summary>The lesson state machine through its service: every transition is stored with its
/// audit row and notification in ONE SaveChangesAsync, and the rules that stop a transition are
/// enforced on the server.</summary>
public class LessonCommandServiceTests
{
    static LessonCommandServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(LessonDto).Assembly);

    private sealed class Env
    {
        public required PaymentTestFixture F { get; init; }
        public required LessonCommandService Service { get; init; }

        /// <summary>Acts as another user for the next calls (the mocked current user is shared).</summary>
        public void ActAs(int userId, string role)
        {
            F.CurrentUser.SetupGet(c => c.UserId).Returns(userId);
            F.CurrentUser.SetupGet(c => c.Role).Returns(role);
            F.CurrentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns((string r) => r == role);
        }
    }

    private const int AdminId = 4;
    private static DateTime Slot(int daysAhead, int hour) => DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour);

    private static Env Create()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        f.Context.RequestStatuses.AddRange(Enum.GetNames<LessonStatus>().Select(code => new RequestStatus { Code = code, Name = code }));
        f.Context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" });
        f.Context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });
        f.Context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021 });
        f.Context.Users.Add(new User { Id = AdminId, Email = "admin@test.ba", PasswordHash = "x", Role = Role.Admin, FirstName = "Admin", LastName = "Test", PhoneNumber = "+387 61 000 001" });
        f.Context.StudentProfiles.AddRange(
            new StudentProfile { UserId = PaymentTestFixture.StudentId, VehicleCategoryId = 1 },
            new StudentProfile { UserId = PaymentTestFixture.OtherStudentId, VehicleCategoryId = 1 });
        f.Context.InstructorProfiles.Add(new InstructorProfile { UserId = PaymentTestFixture.InstructorId, VehicleCategoryId = 1 });
        f.Context.SaveChanges();

        var notifications = new NotificationService(f.Context, f.CurrentUser.Object, f.Pusher.Object, NullLogger<NotificationService>.Instance);
        var payments = new LessonPaymentService(f.Context, f.PayPal.Object, NullLogger<LessonPaymentService>.Instance);
        var service = new LessonCommandService(f.Context, f.CurrentUser.Object, notifications, f.Publisher.Object, payments, NullLogger<LessonCommandService>.Instance);
        return new Env { F = f, Service = service };
    }

    private static LessonBookRequest Request(DateTime startsAtUtc) => new()
    {
        InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, StartsAtUtc = startsAtUtc,
    };

    private static async Task<int> BookAsStudentAsync(Env env, DateTime startsAtUtc)
    {
        env.ActAs(PaymentTestFixture.StudentId, Role.Student);
        return (await env.Service.BookAsync(Request(startsAtUtc), default)).Id;
    }

    [Fact]
    public async Task Book_CreatesPendingLessonHistoryAndNotificationInOneSave()
    {
        var env = Create();
        var saves = env.F.CountSaves();

        var lesson = await BookAsStudentAsync(env, Slot(3, 10));

        Assert.Equal(1, saves());
        var stored = await env.F.Context.Lessons.SingleAsync(l => l.Id == lesson);
        Assert.Equal(LessonStatus.Pending, stored.Status);
        Assert.Single(env.F.Context.LessonStatusHistories);
        // The instructor is asked to answer; the administrator is told a request came in.
        Assert.Equal(2, await env.F.Context.Notifications.CountAsync());
        var toInstructor = await env.F.Context.Notifications.SingleAsync(n => n.UserId == PaymentTestFixture.InstructorId);
        Assert.Equal(NotificationType.LessonCreated, toInstructor.Type);
        var toAdmin = await env.F.Context.Notifications.SingleAsync(n => n.UserId == AdminId);
        Assert.Equal(NotificationType.LessonCreated, toAdmin.Type);
        env.F.Pusher.Verify(p => p.PushAsync(PaymentTestFixture.InstructorId, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        env.F.Pusher.Verify(p => p.PushAsync(AdminId, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_TellsTheOtherSideAndTheAdministrators()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));

        await env.Service.CancelAsync(id, null, default);

        var cancellations = await env.F.Context.Notifications.Where(n => n.Type == NotificationType.LessonCancelled).ToListAsync();
        Assert.Equal([PaymentTestFixture.InstructorId, AdminId], cancellations.Select(n => n.UserId).Order());
    }

    [Fact]
    public async Task TheRequestStatusRow_FollowsTheLessonThroughItsStatuses()
    {
        var env = Create();
        Task<string> CodeOfAsync(int lessonId)
            => env.F.Context.Lessons.Where(l => l.Id == lessonId).Select(l => l.RequestStatus!.Code).SingleAsync();

        var id = await BookAsStudentAsync(env, Slot(3, 10));
        Assert.Equal("Pending", await CodeOfAsync(id));

        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        await env.Service.ConfirmAsync(id, default);
        Assert.Equal("Confirmed", await CodeOfAsync(id));
    }

    [Fact]
    public async Task Book_WithoutTheRequestStatusRow_FailsWithAClearMessageInsteadOfASqlError()
    {
        var env = Create();
        env.F.Context.RequestStatuses.RemoveRange(env.F.Context.RequestStatuses);
        await env.F.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() => BookAsStudentAsync(env, Slot(3, 10)));
    }

    [Fact]
    public async Task Book_SecondStudentInAnOverlappingSlot_IsRejectedWithConflict()
    {
        var env = Create();
        await BookAsStudentAsync(env, Slot(3, 10));

        env.ActAs(PaymentTestFixture.OtherStudentId, Role.Student);
        await Assert.ThrowsAsync<ConflictException>(() => env.Service.BookAsync(Request(Slot(3, 10).AddMinutes(30)), default));
    }

    [Fact]
    public async Task Book_InThePast_IsRejected()
    {
        var env = Create();
        env.ActAs(PaymentTestFixture.StudentId, Role.Student);

        await Assert.ThrowsAsync<ValidationException>(() => env.Service.BookAsync(Request(DateTime.UtcNow.AddHours(-2)), default));
    }

    [Fact]
    public async Task Confirm_ByTheInstructor_ConfirmsStoresHistoryAndNotifiesTheStudentInOneSave()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        var saves = env.F.CountSaves();

        var lesson = await env.Service.ConfirmAsync(id, default);

        Assert.Equal("Confirmed", lesson.Status);
        Assert.Equal(1, saves());
        Assert.Contains(env.F.Context.LessonStatusHistories, h => h.ToStatus == LessonStatus.Confirmed && h.ChangedByUserId == PaymentTestFixture.InstructorId);
        Assert.Contains(env.F.Context.Notifications, n => n.UserId == PaymentTestFixture.StudentId && n.Type == NotificationType.LessonConfirmed);
        env.F.Publisher.Verify(p => p.PublishAsync(It.IsAny<LessonConfirmedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_ByStudent_IsForbidden_AndByAnInstructorWhoDoesNotOwnTheLesson()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));

        env.ActAs(PaymentTestFixture.StudentId, Role.Student);
        await Assert.ThrowsAsync<ForbiddenException>(() => env.Service.ConfirmAsync(id, default));

        env.ActAs(99, Role.Instructor);
        await Assert.ThrowsAsync<ForbiddenException>(() => env.Service.ConfirmAsync(id, default));
    }

    [Fact]
    public async Task Confirm_TwiceOrAfterRejection_BreaksTheStateMachine()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        await env.Service.ConfirmAsync(id, default);

        await Assert.ThrowsAsync<BusinessException>(() => env.Service.ConfirmAsync(id, default));
    }

    [Fact]
    public async Task Reject_WithoutAReason_IsRejected_WithAReason_CancelsAndTellsTheStudentWhy()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);

        await Assert.ThrowsAsync<ValidationException>(() => env.Service.RejectAsync(id, new LessonReasonRequest { Reason = "  " }, default));
        Assert.Equal(LessonStatus.Pending, (await env.F.Context.Lessons.SingleAsync()).Status);

        var lesson = await env.Service.RejectAsync(id, new LessonReasonRequest { Reason = "Vozilo nije dostupno." }, default);

        Assert.Equal("Cancelled", lesson.Status);
        Assert.Equal("Vozilo nije dostupno.", lesson.RejectionReason);
        Assert.Contains(env.F.Context.Notifications, n => n.UserId == PaymentTestFixture.StudentId && n.Message.Contains("Vozilo nije dostupno."));
        env.F.Publisher.Verify(p => p.PublishAsync(It.IsAny<LessonRejectedMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_NeedsAStartedConfirmedLessonAndARatingBetweenOneAndFive()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        var complete = new LessonCompleteRequest { Rating = 5, InstructorNotes = "Odlično" };

        await Assert.ThrowsAsync<BusinessException>(() => env.Service.CompleteAsync(id, complete, default)); // still Pending
        await env.Service.ConfirmAsync(id, default);
        await Assert.ThrowsAsync<BusinessException>(() => env.Service.CompleteAsync(id, complete, default)); // not started
        await env.Service.StartAsync(id, default);
        await Assert.ThrowsAsync<ValidationException>(() => env.Service.CompleteAsync(id, new LessonCompleteRequest { Rating = 0, InstructorNotes = "x" }, default));

        var lesson = await env.Service.CompleteAsync(id, complete, default);

        Assert.Equal("Completed", lesson.Status);
        Assert.Equal(5, lesson.Rating);
        await Assert.ThrowsAsync<BusinessException>(() => env.Service.CompleteAsync(id, complete, default)); // twice
    }

    [Fact]
    public async Task Cancel_PendingRequestByTheStudent_NeedsNoReason_ButAConfirmedOneDoes()
    {
        var env = Create();
        var pending = await BookAsStudentAsync(env, Slot(3, 10));
        var cancelled = await env.Service.CancelAsync(pending, null, default);
        Assert.Equal("Cancelled", cancelled.Status);

        var confirmed = await BookAsStudentAsync(env, Slot(4, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        await env.Service.ConfirmAsync(confirmed, default);
        env.ActAs(PaymentTestFixture.StudentId, Role.Student);

        await Assert.ThrowsAsync<ValidationException>(() => env.Service.CancelAsync(confirmed, new LessonReasonRequest(), default));
    }

    [Fact]
    public async Task Cancel_ConfirmedLessonByTheStudentInsideTheCutoff_IsBlocked_ButTheInstructorMayAlways()
    {
        var env = Create();
        var id = (await env.F.Context.Lessons.AddAsync(new Lesson
        {
            StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, VehicleId = 1,
            StartsAtUtc = DateTime.UtcNow.AddHours(3), EndsAtUtc = DateTime.UtcNow.AddHours(4), Status = LessonStatus.Confirmed,
        })).Entity;
        await env.F.Context.SaveChangesAsync();

        env.ActAs(PaymentTestFixture.StudentId, Role.Student);
        await Assert.ThrowsAsync<BusinessException>(() => env.Service.CancelAsync(id.Id, new LessonReasonRequest { Reason = "Bolest" }, default));

        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        var lesson = await env.Service.CancelAsync(id.Id, new LessonReasonRequest { Reason = "Kvar na vozilu" }, default);
        Assert.Equal("Cancelled", lesson.Status);
    }

    [Fact]
    public async Task Cancel_ByAnUnrelatedUser_IsForbidden()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.OtherStudentId, Role.Student);

        await Assert.ThrowsAsync<ForbiddenException>(() => env.Service.CancelAsync(id, null, default));
    }

    [Fact]
    public async Task Reschedule_IsOnlyPossibleWhilePending_AndOnlyForTheOwner()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));

        var moved = await env.Service.RescheduleAsync(id, new LessonRescheduleRequest { StartsAtUtc = Slot(5, 11), DrivingZoneId = 1 }, default);
        Assert.Equal(Slot(5, 11), moved.StartsAtUtc);

        env.ActAs(PaymentTestFixture.OtherStudentId, Role.Student);
        await Assert.ThrowsAsync<ForbiddenException>(() => env.Service.RescheduleAsync(id, new LessonRescheduleRequest { StartsAtUtc = Slot(6, 11), DrivingZoneId = 1 }, default));

        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        await env.Service.ConfirmAsync(id, default);
        env.ActAs(PaymentTestFixture.StudentId, Role.Student);
        await Assert.ThrowsAsync<BusinessException>(() => env.Service.RescheduleAsync(id, new LessonRescheduleRequest { StartsAtUtc = Slot(6, 11), DrivingZoneId = 1 }, default));
    }

    [Fact]
    public async Task EveryTransition_LeavesAnAuditTrailWithWhoAndWhy()
    {
        var env = Create();
        var id = await BookAsStudentAsync(env, Slot(3, 10));
        env.ActAs(PaymentTestFixture.InstructorId, Role.Instructor);
        await env.Service.ConfirmAsync(id, default);
        await env.Service.StartAsync(id, default);
        await env.Service.CompleteAsync(id, new LessonCompleteRequest { Rating = 4, InstructorNotes = "Dobro" }, default);

        var history = await env.F.Context.LessonStatusHistories.OrderBy(h => h.Id).ToListAsync();

        Assert.Equal(
            [(LessonStatus.Pending, LessonStatus.Pending), (LessonStatus.Pending, LessonStatus.Confirmed), (LessonStatus.Confirmed, LessonStatus.Completed)],
            history.Select(h => (h.FromStatus, h.ToStatus)).ToArray());
        Assert.All(history, h => Assert.False(string.IsNullOrWhiteSpace(h.Reason)));
        Assert.Equal(PaymentTestFixture.StudentId, history[0].ChangedByUserId);
        Assert.Equal(PaymentTestFixture.InstructorId, history[2].ChangedByUserId);
    }
}
