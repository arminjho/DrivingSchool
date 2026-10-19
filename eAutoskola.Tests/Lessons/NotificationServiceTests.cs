using eAutoskola.Application.Notifications;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Tests.Payments;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace eAutoskola.Tests.Lessons;

/// <summary>Notifications join the caller's unit of work: staged on the shared context, stored by
/// the caller's single save, pushed afterwards - and a failing push never breaks the caller.</summary>
public class NotificationServiceTests
{
    static NotificationServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(NotificationService).Assembly);

    private static NotificationService Create(PaymentTestFixture f)
        => new(f.Context, f.CurrentUser.Object, f.Pusher.Object, NullLogger<NotificationService>.Instance);

    [Fact]
    public async Task Stage_OnlyTracksTheRow_NothingIsSavedUntilTheCallerSaves()
    {
        var f = new PaymentTestFixture();
        var saves = f.CountSaves();
        var service = Create(f);

        service.Stage(PaymentTestFixture.StudentId, "Naslov", "Tekst", NotificationType.LessonConfirmed);

        Assert.Equal(0, saves());
        Assert.Equal(0, await f.Context.Notifications.AsNoTracking().CountAsync());

        await f.Context.SaveChangesAsync();
        Assert.Equal(1, await f.Context.Notifications.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task StageForAdmins_ReachesEveryOtherActiveAdministrator_AndNobodyElse()
    {
        var f = new PaymentTestFixture();
        f.Context.Users.AddRange(
            new Domain.Entities.User { Id = 10, Email = "a1@test.ba", PasswordHash = "x", Role = Domain.Security.Role.Admin, FirstName = "A", LastName = "1", PhoneNumber = "+387 61 000 010" },
            new Domain.Entities.User { Id = 11, Email = "a2@test.ba", PasswordHash = "x", Role = Domain.Security.Role.Admin, FirstName = "A", LastName = "2", PhoneNumber = "+387 61 000 011" },
            new Domain.Entities.User { Id = 12, Email = "a3@test.ba", PasswordHash = "x", Role = Domain.Security.Role.Admin, FirstName = "A", LastName = "3", PhoneNumber = "+387 61 000 012", IsActive = false });
        await f.Context.SaveChangesAsync();
        f.CurrentUser.SetupGet(c => c.UserId).Returns(10);
        var service = Create(f);

        await service.StageForAdminsAsync("Naslov", "Tekst", NotificationType.LessonCreated, default);
        await f.Context.SaveChangesAsync();

        // Not the admin who caused it (10), not the deactivated one (12), not a student or instructor.
        Assert.Equal([11], await f.Context.Notifications.Select(n => n.UserId).ToListAsync());
    }

    [Fact]
    public async Task PushStaged_AfterTheSave_PushesEachNotificationOnce_AndNotAgain()
    {
        var f = new PaymentTestFixture();
        var service = Create(f);
        service.Stage(PaymentTestFixture.StudentId, "A", "a", NotificationType.LessonConfirmed);
        service.Stage(PaymentTestFixture.InstructorId, "B", "b", NotificationType.LessonCreated);
        await f.Context.SaveChangesAsync();

        await service.PushStagedAsync(default);
        await service.PushStagedAsync(default);

        f.Pusher.Verify(p => p.PushAsync(PaymentTestFixture.StudentId, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        f.Pusher.Verify(p => p.PushAsync(PaymentTestFixture.InstructorId, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PushStaged_WhenThePushFails_DoesNotThrow()
    {
        var f = new PaymentTestFixture();
        f.Pusher.Setup(p => p.PushAsync(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("hub down"));
        var service = Create(f);
        service.Stage(PaymentTestFixture.StudentId, "A", "a", NotificationType.LessonConfirmed);
        await f.Context.SaveChangesAsync();

        await service.PushStagedAsync(default);

        Assert.Equal(1, await f.Context.Notifications.CountAsync());
    }

    [Fact]
    public async Task DiscardStaged_StopsTheRowsFromBeingSavedLater()
    {
        var f = new PaymentTestFixture();
        var service = Create(f);
        service.Stage(PaymentTestFixture.StudentId, "A", "a", NotificationType.LessonConfirmed);

        service.DiscardStaged();
        await f.Context.SaveChangesAsync();
        await service.PushStagedAsync(default);

        Assert.Equal(0, await f.Context.Notifications.CountAsync());
        f.Pusher.Verify(p => p.PushAsync(It.IsAny<int>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkRead_OnlyForTheOwner()
    {
        var f = new PaymentTestFixture();
        var service = Create(f);
        service.Stage(PaymentTestFixture.OtherStudentId, "A", "a", NotificationType.LessonConfirmed);
        await f.Context.SaveChangesAsync();
        var id = (await f.Context.Notifications.SingleAsync()).Id;

        await Assert.ThrowsAsync<ForbiddenException>(() => service.MarkReadAsync(id, default));
    }
}
