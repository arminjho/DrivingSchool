using eAutoskola.Application.Reports;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Security;
using Microsoft.Extensions.Caching.Memory;

namespace eAutoskola.Tests.Reports;

public class DashboardServiceTests
{
    private static readonly DateTime Now = ReportTestFixture.Utc(2026, 5, 15, 12);

    private static DashboardService CreateService(ReportTestFixture f, IMemoryCache? cache = null)
        => new(f.Context, cache ?? new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task Counts_ActiveStudentsInstructorsAndVehicles_AndNewStudentsThisMonth()
    {
        var f = new ReportTestFixture();
        f.AddStudent(createdAtUtc: ReportTestFixture.Utc(2026, 5, 3));   // new this month
        f.AddStudent(createdAtUtc: ReportTestFixture.Utc(2026, 5, 10));  // new this month
        f.AddStudent(createdAtUtc: ReportTestFixture.Utc(2026, 2, 1));
        f.AddStudent(isActive: false, createdAtUtc: ReportTestFixture.Utc(2026, 5, 4)); // inactive: not counted at all
        f.AddInstructor();
        f.AddInstructor();
        f.AddInstructor(isActive: false);
        f.AddUser(Role.Admin, "Admin", "Korisnik");
        f.AddVehicle("E22-A-123", f.CategoryB);
        f.AddVehicle("E54-K-456", f.CategoryB);
        f.AddVehicle("E88-M-789", f.CategoryB, VehicleStatus.InService);
        f.AddVehicle("E00-X-000", f.CategoryC, isDeleted: true); // soft-deleted vehicles don't count

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(3, dashboard.ActiveStudents);
        Assert.Equal(2, dashboard.NewStudentsThisMonth);
        Assert.Equal(2, dashboard.ActiveInstructors);
        Assert.Equal(1, dashboard.InactiveInstructors);
        Assert.Equal(2, dashboard.ActiveVehicles);
        Assert.Equal(1, dashboard.VehiclesInService);
    }

    [Fact]
    public async Task LessonsThisMonth_ExcludeCancelled_AndComparePercentageWithLastMonth()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 2), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 20), LessonStatus.Confirmed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 25), LessonStatus.Pending);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 26), LessonStatus.Cancelled); // not counted
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 4, 10), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 4, 12), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 4, 14), LessonStatus.Cancelled); // not counted

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(3, dashboard.LessonsThisMonth);
        Assert.Equal(2, dashboard.LessonsLastMonth);
        Assert.Equal(50.0m, dashboard.LessonsChangePercent);
    }

    [Fact]
    public async Task LessonsChangePercent_IsNullWhenLastMonthHadNoLessons()
    {
        var f = new ReportTestFixture();
        f.AddLesson(f.AddStudent(), f.AddInstructor(), ReportTestFixture.Utc(2026, 5, 20), LessonStatus.Confirmed);

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(1, dashboard.LessonsThisMonth);
        Assert.Equal(0, dashboard.LessonsLastMonth);
        Assert.Null(dashboard.LessonsChangePercent);
    }

    [Fact]
    public async Task InJanuary_LastMonthIsDecemberOfThePreviousYear_ButTheChartOnlyShowsTheCurrentYear()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2025, 12, 10), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2025, 12, 11), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 1, 5), LessonStatus.Completed);

        var dashboard = await CreateService(f).BuildAsync(ReportTestFixture.Utc(2026, 1, 20), default);

        Assert.Equal(2, dashboard.LessonsLastMonth);
        Assert.Equal(1, dashboard.LessonsThisMonth);
        Assert.Equal(-50.0m, dashboard.LessonsChangePercent);
        Assert.Equal(1, dashboard.CompletedLessonsPerMonth.Single(m => m.Month == 1).Count);
        Assert.Equal(1, dashboard.CompletedLessonsPerMonth.Sum(m => m.Count));
    }

    [Fact]
    public async Task CompletedLessonsPerMonth_HasTwelveMonths_CountsOnlyCompletedOfThisYear()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 1, 5), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 3, 5), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 3, 9), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 3, 12), LessonStatus.Cancelled);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2025, 3, 12), LessonStatus.Completed); // last year

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(2026, dashboard.Year);
        Assert.Equal(Enumerable.Range(1, 12), dashboard.CompletedLessonsPerMonth.Select(m => m.Month));
        Assert.Equal([1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0], dashboard.CompletedLessonsPerMonth.Select(m => m.Count));
    }

    [Fact]
    public async Task StatusDistribution_CoversAllLessonsEver_WithAnEntryForEveryStatus()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2024, 3, 1), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 6, 1), LessonStatus.Pending);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 6, 2), LessonStatus.Pending);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 6, 3), LessonStatus.Confirmed);

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(["Pending", "Confirmed", "Cancelled", "Completed"], dashboard.StatusDistribution.Select(s => s.Status));
        Assert.Equal([2, 1, 0, 1], dashboard.StatusDistribution.Select(s => s.Count));
    }

    [Fact]
    public async Task TodayLessons_AreUnfinishedPendingOrConfirmedLessonsOfToday_InStartOrder()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent("Amina", "Hodžić");
        var instructor = f.AddInstructor("Damir", "Kovačević");
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 15, 9), LessonStatus.Confirmed);   // already finished
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 15, 16), LessonStatus.Pending);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 15, 14), LessonStatus.Confirmed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 15, 15), LessonStatus.Cancelled); // not on
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 16, 10), LessonStatus.Confirmed); // tomorrow

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal([14, 16], dashboard.TodayLessons.Select(l => l.StartsAtUtc.Hour));
        var first = dashboard.TodayLessons[0];
        Assert.Equal("Amina Hodžić", first.StudentFullName);
        Assert.Equal("Damir Kovačević", first.InstructorFullName);
        Assert.Equal("Centar", first.DrivingZoneName);
        Assert.Equal("Confirmed", first.Status);
    }

    [Fact]
    public async Task RecentActivity_MergesAllFourKindsNewestFirst_AndIsCappedAtTen()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent("Amina", "Hodžić", createdAtUtc: ReportTestFixture.Utc(2026, 5, 1));
        var instructor = f.AddUser(Role.Instructor, "Damir", "Kovačević", createdAtUtc: ReportTestFixture.Utc(2026, 4, 1));
        var lesson = f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 5, 20), LessonStatus.Confirmed, createdAtUtc: ReportTestFixture.Utc(2026, 5, 10));
        var vehicle = f.AddVehicle("E22-A-123", f.CategoryB);
        f.AddService(vehicle, ReportTestFixture.Utc(2026, 6, 1), 80m, VehicleServiceStatus.Scheduled, createdAtUtc: ReportTestFixture.Utc(2026, 5, 12));
        f.Context.LessonStatusHistories.AddRange(
            new LessonStatusHistory { LessonId = lesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Pending, ChangedByUserId = student.Id, ChangedAt = ReportTestFixture.Utc(2026, 5, 14) }, // not a decision
            new LessonStatusHistory { LessonId = lesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = instructor.Id, ChangedAt = ReportTestFixture.Utc(2026, 5, 13) });
        f.Context.SaveChanges();

        var activity = (await CreateService(f).BuildAsync(Now, default)).RecentActivity;

        Assert.Equal(
            [nameof(ActivityType.LessonConfirmed), nameof(ActivityType.VehicleServiceAdded), nameof(ActivityType.LessonRequested), nameof(ActivityType.UserCreated)],
            activity.Take(4).Select(a => a.Type));
        Assert.Equal("Čas odobren", activity[0].Title);
        Assert.Contains("E22-A-123", activity[1].Description);
        Assert.Equal(activity.OrderByDescending(a => a.OccurredAtUtc), activity);
    }

    [Fact]
    public async Task RecentActivity_NeverExceedsTen()
    {
        var f = new ReportTestFixture();
        for (var i = 0; i < 15; i++)
        {
            f.AddStudent(createdAtUtc: ReportTestFixture.Utc(2026, 5, 1).AddHours(i));
        }

        var dashboard = await CreateService(f).BuildAsync(Now, default);

        Assert.Equal(10, dashboard.RecentActivity.Count);
    }

    [Fact]
    public async Task GetAsync_ServesTheCachedResultWithinTheCacheWindow()
    {
        var f = new ReportTestFixture();
        f.AddStudent();
        var service = CreateService(f);

        var first = await service.GetAsync(default);
        f.AddStudent();
        var second = await service.GetAsync(default);

        Assert.Same(first, second);
        Assert.Equal(1, second.ActiveStudents); // the second student isn't visible until the entry expires
    }
}
