namespace eAutoskola.Application.Lessons;

/// <summary>
/// The one definition of the bookable slot grid: instructor working hours (UTC) and the length
/// of a lesson. LessonQueryService offers slots from it and LessonCommandService uses the same
/// slot length for a lesson's end, so the two can never disagree. Simple constants, not
/// per-instructor configurable.
/// </summary>
public static class LessonSlotPolicy
{
    public static readonly TimeSpan WorkDayStart = TimeSpan.FromHours(8);
    public static readonly TimeSpan WorkDayEnd = TimeSpan.FromHours(18);
    public static readonly TimeSpan SlotDuration = TimeSpan.FromHours(1);
}
