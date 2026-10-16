using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;

namespace eAutoskola.Application.Lessons;

/// <summary>
/// The ONLY place Lesson status transitions are decided (upute.md §7) - pure, no I/O. Called
/// by LessonCommandService; never duplicated in a controller.
///
/// Allowed transitions (see docs/REFERENCE_ANALYSIS.md / the approved plan's state table):
///   Pending   -> Confirmed  (Instructor who owns the lesson, or Admin)
///   Pending   -> Cancelled  (Student cancels own request; or Instructor rejects - reason required)
///   Confirmed -> Cancelled  (Student before the cutoff, Instructor, or Admin - reason required)
///   Confirmed -> Completed  (Instructor; requires Rating 1-5 + InstructorNotes; StartedAt must
///                            already be set via a prior /start call)
/// Any other pair -> BusinessException. Lessons are never hard-deleted.
/// </summary>
public static class LessonStateMachine
{
    /// <summary>Student cancellation of a Confirmed lesson is only allowed this many hours
    /// before the scheduled start.</summary>
    public const int CancellationCutoffHours = 24;

    private static readonly Dictionary<LessonStatus, LessonStatus[]> AllowedTransitions = new()
    {
        [LessonStatus.Pending] = [LessonStatus.Confirmed, LessonStatus.Cancelled],
        [LessonStatus.Confirmed] = [LessonStatus.Cancelled, LessonStatus.Completed],
    };

    public static void EnsureCanTransition(LessonStatus from, LessonStatus to)
    {
        if (!AllowedTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
        {
            throw new BusinessException($"Prelaz iz stanja '{from}' u stanje '{to}' nije dozvoljen.");
        }
    }
}
