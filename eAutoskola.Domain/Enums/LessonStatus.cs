namespace eAutoskola.Domain.Enums;

/// <summary>
/// Drives the centralized state machine (eAutoskola.Application/Lessons/LessonStateMachine).
/// Allowed transitions: Pending -> Confirmed, Pending -> Cancelled, Confirmed -> Cancelled,
/// Confirmed -> Completed. Never hard-deleted (upute.md §7).
/// </summary>
public enum LessonStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
}
