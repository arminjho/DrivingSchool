namespace eAutoskola.Application.Reports;

/// <summary>Everything the admin dashboard shows (docs/prijava.md §4.1.1).</summary>
public class DashboardDto
{
    public int ActiveStudents { get; set; }
    public int NewStudentsThisMonth { get; set; }

    public int ActiveInstructors { get; set; }
    public int InactiveInstructors { get; set; }

    public int ActiveVehicles { get; set; }
    public int VehiclesInService { get; set; }

    /// <summary>Lessons starting this month that are not cancelled (pending, confirmed, completed).</summary>
    public int LessonsThisMonth { get; set; }
    public int LessonsLastMonth { get; set; }

    /// <summary>Percent change vs last month; null when last month had no lessons to compare with.</summary>
    public decimal? LessonsChangePercent { get; set; }

    public int Year { get; set; }

    /// <summary>Always 12 entries (January to December) for <see cref="Year"/>.</summary>
    public List<MonthlyLessonCountDto> CompletedLessonsPerMonth { get; set; } = [];

    /// <summary>All lessons ever, always one entry per status.</summary>
    public List<LessonStatusCountDto> StatusDistribution { get; set; } = [];

    public List<RecentActivityDto> RecentActivity { get; set; } = [];
    public List<TodayLessonDto> TodayLessons { get; set; } = [];

    public DateTime GeneratedAtUtc { get; set; }
}

public class MonthlyLessonCountDto
{
    public int Month { get; set; }
    public int Count { get; set; }
}

public class LessonStatusCountDto
{
    public required string Status { get; set; }
    public int Count { get; set; }
}

public enum ActivityType
{
    LessonRequested,
    LessonConfirmed,
    LessonCancelled,
    LessonCompleted,
    VehicleServiceAdded,
    UserCreated,
}

public class RecentActivityDto
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public class TodayLessonDto
{
    public int LessonId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public required string StudentFullName { get; set; }
    public required string InstructorFullName { get; set; }
    public required string DrivingZoneName { get; set; }
    public required string Status { get; set; }
}
