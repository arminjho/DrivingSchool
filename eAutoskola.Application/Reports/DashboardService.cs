using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eAutoskola.Application.Reports;

/// <summary>
/// Admin dashboard numbers. Every aggregate is one GroupBy query (users, vehicles, lessons per
/// month/status, status distribution) rather than a Count per figure, and the finished
/// result is cached briefly so a burst of dashboard refreshes costs one database round.
/// </summary>
public class DashboardService
{
    private const string CacheKey = "dashboard:summary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private const int RecentActivityCount = 10;
    private const int MaxTodayLessons = 20;

    private readonly EAutoskolaDbContext _context;
    private readonly IMemoryCache _cache;

    public DashboardService(EAutoskolaDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out DashboardDto? cached) && cached is not null)
        {
            return cached;
        }

        var dashboard = await BuildAsync(DateTime.UtcNow, cancellationToken);
        _cache.Set(CacheKey, dashboard, CacheDuration);
        return dashboard;
    }

    /// <summary>The cache-free calculation; the clock is passed in so month boundaries can be tested.</summary>
    public async Task<DashboardDto> BuildAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        var yearStart = new DateTime(nowUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var userGroups = await _context.Users.AsNoTracking()
            .GroupBy(u => new { u.Role, u.IsActive })
            .Select(g => new { g.Key.Role, g.Key.IsActive, NewThisMonth = g.Count(u => u.CreatedAt >= monthStart), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var vehicleGroups = await _context.Vehicles.AsNoTracking()
            .GroupBy(v => v.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var windowStart = lastMonthStart < yearStart ? lastMonthStart : yearStart;
        var monthlyGroups = await _context.Lessons.AsNoTracking()
            .Where(l => l.StartsAtUtc >= windowStart && l.StartsAtUtc < yearStart.AddYears(1))
            .GroupBy(l => new { l.StartsAtUtc.Year, l.StartsAtUtc.Month, l.Status })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusGroups = await _context.Lessons.AsNoTracking()
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lessonsThisMonth = CountScheduledInMonth(monthlyGroups.Select(g => (g.Year, g.Month, g.Status, g.Count)), nowUtc.Year, nowUtc.Month);
        var lessonsLastMonth = CountScheduledInMonth(monthlyGroups.Select(g => (g.Year, g.Month, g.Status, g.Count)), lastMonthStart.Year, lastMonthStart.Month);

        return new DashboardDto
        {
            ActiveStudents = userGroups.Where(g => g.Role == Role.Student && g.IsActive).Sum(g => g.Count),
            NewStudentsThisMonth = userGroups.Where(g => g.Role == Role.Student && g.IsActive).Sum(g => g.NewThisMonth),
            ActiveInstructors = userGroups.Where(g => g.Role == Role.Instructor && g.IsActive).Sum(g => g.Count),
            InactiveInstructors = userGroups.Where(g => g.Role == Role.Instructor && !g.IsActive).Sum(g => g.Count),
            ActiveVehicles = vehicleGroups.Where(g => g.Status == VehicleStatus.Active).Sum(g => g.Count),
            VehiclesInService = vehicleGroups.Where(g => g.Status == VehicleStatus.InService).Sum(g => g.Count),
            LessonsThisMonth = lessonsThisMonth,
            LessonsLastMonth = lessonsLastMonth,
            LessonsChangePercent = lessonsLastMonth == 0
                ? null
                : Math.Round((lessonsThisMonth - lessonsLastMonth) * 100m / lessonsLastMonth, 1),
            Year = nowUtc.Year,
            CompletedLessonsPerMonth = Enumerable.Range(1, 12).Select(month => new MonthlyLessonCountDto
            {
                Month = month,
                Count = monthlyGroups
                    .Where(g => g.Year == nowUtc.Year && g.Month == month && g.Status == LessonStatus.Completed)
                    .Sum(g => g.Count),
            }).ToList(),
            StatusDistribution = Enum.GetValues<LessonStatus>().Select(status => new LessonStatusCountDto
            {
                Status = status.ToString(),
                Count = statusGroups.Where(g => g.Status == status).Sum(g => g.Count),
            }).ToList(),
            RecentActivity = await GetRecentActivityAsync(cancellationToken),
            TodayLessons = await GetTodayLessonsAsync(nowUtc, cancellationToken),
            GeneratedAtUtc = nowUtc,
        };
    }

    private static int CountScheduledInMonth(IEnumerable<(int Year, int Month, LessonStatus Status, int Count)> groups, int year, int month)
        => groups.Where(g => g.Year == year && g.Month == month && g.Status != LessonStatus.Cancelled).Sum(g => g.Count);

    /// <summary>Four small "latest N" queries merged by time - new requests, status changes, new
    /// vehicle services and new users - so the feed shows the newest overall, whichever kind.</summary>
    private async Task<List<RecentActivityDto>> GetRecentActivityAsync(CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters(): this is a feed of the most recent events system-wide, not a
        // listing of currently-active students/instructors/vehicles. A student, instructor or
        // vehicle referenced by a recent event may since have been soft-deleted (User/Vehicle's
        // own IsDeleted filter) - that must not silently drop the event from the feed or shrink
        // it below RecentActivityCount (docs/BACKLOG.md "Phase 17 audit").
        var requests = await _context.Lessons.IgnoreQueryFilters().AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(RecentActivityCount)
            .Select(l => new
            {
                l.CreatedAt,
                Student = l.Student!.FirstName + " " + l.Student.LastName,
                Instructor = l.Instructor!.FirstName + " " + l.Instructor.LastName,
            })
            .ToListAsync(cancellationToken);

        // From == To rows are reschedules/notes, not decisions; a Pending target can't be a decision either.
        var decisions = await _context.LessonStatusHistories.IgnoreQueryFilters().AsNoTracking()
            .Where(h => h.FromStatus != h.ToStatus && h.ToStatus != LessonStatus.Pending)
            .OrderByDescending(h => h.ChangedAt)
            .Take(RecentActivityCount)
            .Select(h => new
            {
                h.ToStatus,
                h.ChangedAt,
                Student = h.Lesson!.Student!.FirstName + " " + h.Lesson.Student.LastName,
                Instructor = h.Lesson.Instructor!.FirstName + " " + h.Lesson.Instructor.LastName,
            })
            .ToListAsync(cancellationToken);

        var services = await _context.VehicleServices.IgnoreQueryFilters().AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(RecentActivityCount)
            .Select(s => new { s.CreatedAt, s.Vehicle!.RegistrationNumber, ServiceType = s.VehicleServiceType!.Name, s.ServiceDate })
            .ToListAsync(cancellationToken);

        var users = await _context.Users.AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(RecentActivityCount)
            .Select(u => new { u.CreatedAt, Name = u.FirstName + " " + u.LastName, u.Role })
            .ToListAsync(cancellationToken);

        return requests.Select(r => new RecentActivityDto
            {
                Type = nameof(ActivityType.LessonRequested),
                Title = ReportTexts.ActivityLessonRequested,
                Description = $"{r.Student} - instruktor {r.Instructor}",
                OccurredAtUtc = r.CreatedAt,
            })
            .Concat(decisions.Select(d => new RecentActivityDto
            {
                Type = d.ToStatus switch
                {
                    LessonStatus.Confirmed => nameof(ActivityType.LessonConfirmed),
                    LessonStatus.Completed => nameof(ActivityType.LessonCompleted),
                    _ => nameof(ActivityType.LessonCancelled),
                },
                Title = d.ToStatus switch
                {
                    LessonStatus.Confirmed => ReportTexts.ActivityLessonConfirmed,
                    LessonStatus.Completed => ReportTexts.ActivityLessonCompleted,
                    _ => ReportTexts.ActivityLessonCancelled,
                },
                Description = $"{d.Student} - instruktor {d.Instructor}",
                OccurredAtUtc = d.ChangedAt,
            }))
            .Concat(services.Select(s => new RecentActivityDto
            {
                Type = nameof(ActivityType.VehicleServiceAdded),
                Title = ReportTexts.ActivityVehicleService,
                Description = $"{s.RegistrationNumber} - {s.ServiceType} ({ReportTexts.FormatDate(s.ServiceDate)})",
                OccurredAtUtc = s.CreatedAt,
            }))
            .Concat(users.Select(u => new RecentActivityDto
            {
                Type = nameof(ActivityType.UserCreated),
                Title = ReportTexts.ActivityUserCreated,
                Description = $"{u.Name} ({ReportTexts.RoleLabel(u.Role)})",
                OccurredAtUtc = u.CreatedAt,
            }))
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(RecentActivityCount)
            .ToList();
    }

    /// <summary>Today's lessons that haven't finished yet and are still on (pending or confirmed).</summary>
    private async Task<List<TodayLessonDto>> GetTodayLessonsAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var todayStart = nowUtc.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var lessons = await _context.Lessons.AsNoTracking()
            .Where(l => l.StartsAtUtc >= todayStart && l.StartsAtUtc < tomorrowStart && l.EndsAtUtc > nowUtc
                && (l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed))
            .OrderBy(l => l.StartsAtUtc)
            .Take(MaxTodayLessons)
            .Select(l => new
            {
                l.Id,
                l.StartsAtUtc,
                l.EndsAtUtc,
                Student = l.Student!.FirstName + " " + l.Student.LastName,
                Instructor = l.Instructor!.FirstName + " " + l.Instructor.LastName,
                Zone = l.DrivingZone!.Name,
                l.Status,
            })
            .ToListAsync(cancellationToken);

        return lessons.Select(l => new TodayLessonDto
        {
            LessonId = l.Id,
            StartsAtUtc = l.StartsAtUtc,
            EndsAtUtc = l.EndsAtUtc,
            StudentFullName = l.Student,
            InstructorFullName = l.Instructor,
            DrivingZoneName = l.Zone,
            Status = l.Status.ToString(),
        }).ToList();
    }
}
