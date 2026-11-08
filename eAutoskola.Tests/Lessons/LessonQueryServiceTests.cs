using eAutoskola.Application.Lessons;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Security;
using eAutoskola.Tests.Payments;
using Mapster;

namespace eAutoskola.Tests.Lessons;

/// <summary>The order of the lesson lists: the student's history puts the newest request first (so a
/// just-booked lesson is the first card after the redirect), while "next lesson" still asks for the
/// start time explicitly.</summary>
public class LessonQueryServiceTests
{
    static LessonQueryServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(LessonDto).Assembly);

    private static readonly DateTime Today = DateTime.UtcNow.Date;

    /// <summary>Three active lessons of the student: booked in the order 1, 2, 3 but starting in the
    /// order 2, 1, 3.</summary>
    private static (LessonQueryService Service, int[] Ids) Create(int currentUserId, string role)
    {
        var f = new PaymentTestFixture(currentUserId, role);
        f.Context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });

        Lesson NewLesson(int startsInDays, int bookedMinutesAgo) => new()
        {
            StudentId = PaymentTestFixture.StudentId,
            InstructorId = PaymentTestFixture.InstructorId,
            DrivingZoneId = 1,
            StartsAtUtc = Today.AddDays(startsInDays).AddHours(10),
            EndsAtUtc = Today.AddDays(startsInDays).AddHours(11),
            LessonNumber = 1,
            Status = LessonStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-bookedMinutesAgo),
        };

        var lessons = new[] { NewLesson(5, 30), NewLesson(2, 20), NewLesson(9, 10) };
        f.Context.Lessons.AddRange(lessons);
        f.Context.SaveChanges();

        return (new LessonQueryService(f.Context, f.CurrentUser.Object), lessons.Select(l => l.Id).ToArray());
    }

    [Fact]
    public async Task MyLessons_DefaultOrder_IsNewestRequestFirst()
    {
        var (service, ids) = Create(PaymentTestFixture.StudentId, Role.Student);

        var page = await service.GetMyLessonsAsync(new MyLessonsSearchObject(), default);

        Assert.Equal([ids[2], ids[1], ids[0]], page.ResultList.Select(l => l.Id));
    }

    [Fact]
    public async Task MyLessons_AskingForStartTimeAscending_FindsTheNextLessonFirst()
    {
        var (service, ids) = Create(PaymentTestFixture.StudentId, Role.Student);

        var page = await service.GetMyLessonsAsync(new MyLessonsSearchObject { SortBy = "StartsAtUtc", SortDirection = "asc" }, default);

        Assert.Equal([ids[1], ids[0], ids[2]], page.ResultList.Select(l => l.Id));
    }

    [Fact]
    public async Task TheLessonCarriesItsVehicleSoTheAppsCanShowItByNameAndRegistration()
    {
        var f = new PaymentTestFixture();
        f.Context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });
        f.Context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" });
        f.Context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021 });
        f.Context.Lessons.Add(new Lesson
        {
            StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1, VehicleId = 1,
            StartsAtUtc = Today.AddDays(2).AddHours(10), EndsAtUtc = Today.AddDays(2).AddHours(11),
        });
        f.Context.SaveChanges();
        var service = new LessonQueryService(f.Context, f.CurrentUser.Object);

        var lesson = (await service.GetMyLessonsAsync(new MyLessonsSearchObject(), default)).ResultList.Single();

        Assert.Equal("E22-A-123", lesson.VehicleRegistrationNumber);
        Assert.Equal("VW", lesson.VehicleMake);
        Assert.Equal("Polo", lesson.VehicleModel);
    }

    [Fact]
    public async Task InstructorRequests_IgnoreACreatedAtSort_TheirColumnAllowlistOnlyHasTheStartTime()
    {
        var (service, ids) = Create(PaymentTestFixture.InstructorId, Role.Instructor);

        var page = await service.GetLessonRequestsAsync(new LessonRequestSearchObject { SortBy = "CreatedAt", SortDirection = "desc" }, default);

        Assert.Equal([ids[2], ids[0], ids[1]], page.ResultList.Select(l => l.Id));
    }

    /// <summary>Dorada #16: a request the instructor never acted on (Pending), or a confirmed
    /// lesson whose time already passed without being marked Completed, must never be mistaken
    /// for the "next lesson" - but the general "active" list still shows both, unchanged.</summary>
    [Fact]
    public async Task Upcoming_ExcludesAPendingOrConfirmedLessonWhoseStartHasAlreadyPassed()
    {
        var f = new PaymentTestFixture();
        f.Context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });
        var overdue = new Lesson
        {
            StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1,
            StartsAtUtc = DateTime.UtcNow.AddDays(-1), EndsAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(1), Status = LessonStatus.Confirmed,
        };
        var upcoming = new Lesson
        {
            StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1,
            StartsAtUtc = DateTime.UtcNow.AddDays(3), EndsAtUtc = DateTime.UtcNow.AddDays(3).AddHours(1), Status = LessonStatus.Pending,
        };
        f.Context.Lessons.AddRange(overdue, upcoming);
        f.Context.SaveChanges();
        var service = new LessonQueryService(f.Context, f.CurrentUser.Object);

        var active = await service.GetMyLessonsAsync(new MyLessonsSearchObject { Tab = "active" }, default);
        var upcomingTab = await service.GetMyLessonsAsync(new MyLessonsSearchObject { Tab = "upcoming" }, default);

        Assert.Equal(2, active.ResultList.Count);
        Assert.Equal([upcoming.Id], upcomingTab.ResultList.Select(l => l.Id));
    }

    [Fact]
    public async Task Upcoming_SortedByStartAscending_ReturnsTheNearestFutureLessonFirst_EvenWithAnOverdueOneInTheList()
    {
        var f = new PaymentTestFixture();
        f.Context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });

        Lesson NewLesson(DateTime startsAtUtc, LessonStatus status) => new()
        {
            StudentId = PaymentTestFixture.StudentId, InstructorId = PaymentTestFixture.InstructorId, DrivingZoneId = 1,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddHours(1), Status = status,
        };

        var overdue = NewLesson(DateTime.UtcNow.AddHours(-2), LessonStatus.Confirmed);
        var nearest = NewLesson(DateTime.UtcNow.AddDays(1), LessonStatus.Pending);
        var later = NewLesson(DateTime.UtcNow.AddDays(5), LessonStatus.Confirmed);
        f.Context.Lessons.AddRange(overdue, nearest, later);
        f.Context.SaveChanges();
        var service = new LessonQueryService(f.Context, f.CurrentUser.Object);

        var page = await service.GetMyLessonsAsync(new MyLessonsSearchObject { Tab = "upcoming", SortBy = "StartsAtUtc", SortDirection = "asc" }, default);

        Assert.Equal([nearest.Id, later.Id], page.ResultList.Select(l => l.Id));
    }
}
