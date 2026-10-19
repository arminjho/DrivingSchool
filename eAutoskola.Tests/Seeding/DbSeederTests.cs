using eAutoskola.Application.Recommendations;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Seeding;

/// <summary>Seeds once, then checks what a tester will rely on: volume, the demo accounts, a
/// consistent calendar (nothing booked twice), a status history for every lesson, a payment
/// for every paid installment, and images that really exist.</summary>
public class SeededDatabase : IAsyncLifetime
{
    public EAutoskolaDbContext Context { get; } = new(
        new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public async Task InitializeAsync()
    {
        await DbSeeder.SeedAsync(Context);
        await RecommendationLogSeeder.SeedAsync(Context);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public class DbSeederTests(SeededDatabase db) : IClassFixture<SeededDatabase>
{
    private EAutoskolaDbContext Context => db.Context;

    [Fact]
    public async Task TheDemoAdministrator_HasNotificationsToTryTheBellWith()
    {
        var notifications = await Context.Notifications.Where(n => n.User!.Email == "desktop").ToListAsync();

        Assert.True(notifications.Count >= 6);
        Assert.Contains(notifications, n => !n.IsRead);
        Assert.Contains(notifications, n => n.IsRead);
    }

    [Fact]
    public async Task EveryLesson_PointsAtTheRequestStatusRowOfItsStatus()
    {
        var mismatched = await Context.Lessons.Include(l => l.RequestStatus)
            .Where(l => l.RequestStatus == null)
            .CountAsync();
        Assert.Equal(0, mismatched);

        var lessons = await Context.Lessons.Include(l => l.RequestStatus).ToListAsync();
        Assert.All(lessons, lesson => Assert.Equal(lesson.Status.ToString(), lesson.RequestStatus!.Code));
    }

    [Fact]
    public async Task EveryMainTable_HasEnoughRowsToTestTheApp()
    {
        Assert.True(await Context.Users.CountAsync() >= 30);
        Assert.True(await Context.StudentProfiles.CountAsync() >= 20);
        Assert.True(await Context.InstructorProfiles.CountAsync() >= 5);
        Assert.True(await Context.Vehicles.CountAsync() >= 12);
        Assert.True(await Context.VehicleServices.CountAsync() >= 15);
        Assert.True(await Context.Lessons.CountAsync() >= 200);
        Assert.True(await Context.LessonStatusHistories.CountAsync() >= 400);
        Assert.True(await Context.Charges.CountAsync() >= 60);
        Assert.True(await Context.Installments.CountAsync() >= 180);
        Assert.True(await Context.Payments.CountAsync() >= 30);
        Assert.True(await Context.Refunds.CountAsync() >= 1);
        Assert.True(await Context.Notifications.CountAsync() >= 30);
        Assert.Equal(6, await Context.Tests.CountAsync());
        Assert.Equal(60, await Context.Questions.CountAsync());
        Assert.Equal(240, await Context.AnswerOptions.CountAsync());
        Assert.True(await Context.TestAttempts.CountAsync() >= 30);
        Assert.True(await Context.AttemptAnswers.CountAsync() >= 300);
        Assert.Equal(12, await Context.StudyLessons.CountAsync());
        Assert.True(await Context.StudyLessonSections.CountAsync() >= 50);
        Assert.True(await Context.StudentStudyLessons.CountAsync() >= 15);
        Assert.True(await Context.RecommendationLogs.CountAsync() >= 10);
        Assert.True(await Context.Cities.CountAsync() >= 10);
        Assert.Contains(await Context.VehicleCategories.Select(c => c.Code).ToListAsync(), code => code == "E");
    }

    [Theory]
    [InlineData("desktop", Role.Admin)]
    [InlineData("mobile", Role.Student)]
    [InlineData("instruktor", Role.Instructor)]
    public async Task DemoAccounts_ExistWithTheirRoleAndThePasswordTest(string login, string role)
    {
        var user = await Context.Users.SingleAsync(u => u.Email == login);

        Assert.Equal(role, user.Role);
        Assert.True(user.IsActive);
        Assert.True(PasswordHasher.Verify("test", user.PasswordHash));
    }

    [Fact]
    public async Task EveryUser_HasAKnownRole_AndEveryStudentAndInstructorAProfile()
    {
        var users = await Context.Users.Include(u => u.StudentProfile).Include(u => u.InstructorProfile).ToListAsync();

        Assert.All(users, u => Assert.Contains(u.Role, Role.All));
        Assert.All(users.Where(u => u.Role == Role.Student), u => Assert.NotNull(u.StudentProfile));
        Assert.All(users.Where(u => u.Role == Role.Instructor), u => Assert.NotNull(u.InstructorProfile));
        Assert.Equal(users.Count, users.Select(u => u.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public async Task Calendar_NoInstructorStudentOrVehicleIsBookedTwiceAtTheSameTime()
    {
        var open = await Context.Lessons
            .Where(l => l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed)
            .ToListAsync();

        Assert.Equal(open.Count, open.Select(l => (l.InstructorId, l.StartsAtUtc)).Distinct().Count());
        Assert.Equal(open.Count, open.Select(l => (l.StudentId, l.StartsAtUtc)).Distinct().Count());
        Assert.Equal(open.Count, open.Select(l => (l.VehicleId, l.StartsAtUtc)).Distinct().Count());
    }

    [Fact]
    public async Task EveryLesson_HasAnAuditTrailEndingInItsCurrentStatus_AndTheFieldsItsStatusNeeds()
    {
        var lessons = await Context.Lessons.Include(l => l.StatusHistory).ToListAsync();

        foreach (var lesson in lessons)
        {
            var last = lesson.StatusHistory.OrderBy(h => h.ChangedAt).ThenBy(h => h.Id).Last();
            Assert.Equal(lesson.Status, last.ToStatus);
            Assert.NotEmpty(lesson.StatusHistory);
            Assert.True(lesson.LessonNumber >= 1);

            if (lesson.Status == LessonStatus.Completed)
            {
                Assert.InRange(lesson.Rating ?? 0, 1, 5);
                Assert.NotNull(lesson.CompletedAt);
                Assert.False(string.IsNullOrWhiteSpace(lesson.InstructorNotes));
            }

            if (lesson.Status == LessonStatus.Cancelled)
            {
                Assert.False(string.IsNullOrWhiteSpace(lesson.RejectionReason));
            }
        }
    }

    [Fact]
    public async Task Lessons_AreSpreadOverTheYear_ForTheDashboardChart()
    {
        var completedMonths = await Context.Lessons
            .Where(l => l.Status == LessonStatus.Completed)
            .Select(l => l.StartsAtUtc.Month)
            .Distinct()
            .CountAsync();

        Assert.True(completedMonths >= 5);
        Assert.Equal(3, await Context.Vehicles.CountAsync(v => v.Status == VehicleStatus.InService));
    }

    [Fact]
    public async Task PaidInstallments_HaveACompletedPayment_WithUniquePayPalIds()
    {
        var paid = await Context.Installments.Include(i => i.Payments).Where(i => i.Status == InstallmentStatus.Paid).ToListAsync();

        Assert.NotEmpty(paid);
        Assert.All(paid, i => Assert.Single(i.Payments, p => p.Status == PaymentStatus.Completed && p.Amount == i.Amount));

        var orderIds = await Context.Payments.Select(p => p.PayPalOrderId).ToListAsync();
        Assert.Equal(orderIds.Count, orderIds.Distinct().Count());
    }

    [Fact]
    public async Task EveryStudent_HasThePackage_AndAdnanHasThePaymentScenarios()
    {
        var students = await Context.Users.Where(u => u.Role == Role.Student).ToListAsync();
        var packageCharges = await Context.Charges.Include(c => c.Installments).Where(c => c.LessonId == null).ToListAsync();

        Assert.All(students, s => Assert.Equal(3, packageCharges.Count(c => c.StudentId == s.Id)));
        Assert.All(packageCharges, c => Assert.Equal(PaymentConstants.PackageInstallmentCount, c.Installments.Count));

        var adnan = students.Single(s => s.Email == "adnan.zukic@eautoskola.ba");
        var extras = await Context.Charges.Include(c => c.Installments).Where(c => c.StudentId == adnan.Id && c.LessonId != null).ToListAsync();
        Assert.Contains(extras, c => c.Status == ChargeStatus.Cancelled && c.Installments.Single().Status == InstallmentStatus.Cancelled);
        Assert.Contains(extras, c => c.Status == ChargeStatus.Active && c.Installments.Single().Status == InstallmentStatus.Pending);
        Assert.Equal(PaymentConstants.PackagePracticalLessonQuota + 2, await Context.Lessons.CountAsync(l => l.StudentId == adnan.Id));
    }

    [Fact]
    public async Task DemoStudent_MatchesTheMockups_TwelveLessonsDoneAndTestResultsOf70_90_50_80()
    {
        var mobile = await Context.Users.SingleAsync(u => u.Email == "mobile");

        Assert.Equal(12, await Context.Lessons.CountAsync(l => l.StudentId == mobile.Id && l.Status == LessonStatus.Completed));
        Assert.True(await Context.Lessons.AnyAsync(l => l.StudentId == mobile.Id && l.Status == LessonStatus.Confirmed && l.StartsAtUtc > DateTime.UtcNow));

        var lastPercents = (await Context.TestAttempts.Where(a => a.StudentId == mobile.Id).ToListAsync())
            .GroupBy(a => a.TestId)
            .Select(g => g.OrderByDescending(a => a.SubmittedAt).First().Percent)
            .OrderBy(p => p)
            .ToArray();
        Assert.Equal([50.0, 70.0, 80.0, 90.0], lastPercents);

        var log = await Context.RecommendationLogs.FirstAsync(r => r.StudentId == mobile.Id);
        Assert.Contains("Preporučujemo", log.Explanation);
    }

    [Fact]
    public async Task EveryImagePath_PointsToAFileThatExists_AndEveryTestQuestionHasFourOptionsWithOneCorrect()
    {
        var paths = (await Context.Users.Select(u => u.ProfileImagePath).ToListAsync())
            .Concat(await Context.Vehicles.Select(v => v.ImagePath).ToListAsync())
            .Concat(await Context.Questions.Select(q => q.ImagePath).ToListAsync())
            .Where(p => p is not null)
            .ToList();

        Assert.True(paths.Count >= 25);
        Assert.All(paths, p => Assert.True(File.Exists(p), $"missing seed image {p}"));
        Assert.Equal(paths.Count, paths.Distinct().Count());

        var questions = await Context.Questions.Include(q => q.AnswerOptions).ToListAsync();
        Assert.All(questions, q =>
        {
            Assert.Equal(4, q.AnswerOptions.Count);
            Assert.Single(q.AnswerOptions, o => o.IsCorrect);
        });
        Assert.All(await Context.Tests.Include(t => t.Questions).ToListAsync(), t => Assert.Equal(10, t.Questions.Count));
    }

    [Fact]
    public async Task Seeding_IsIdempotent()
    {
        var before = await Context.Users.CountAsync();

        await DbSeeder.SeedAsync(Context);
        await RecommendationLogSeeder.SeedAsync(Context);

        Assert.Equal(before, await Context.Users.CountAsync());
    }
}
