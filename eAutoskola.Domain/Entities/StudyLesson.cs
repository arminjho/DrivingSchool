namespace eAutoskola.Domain.Entities;

/// <summary>Soft-deleted like Vehicle/User, never hard-deleted: a lesson that was ever opened or
/// recommended is kept by StudentStudyLesson/RecommendationLog (the latter via
/// DeleteBehavior.Restrict), so a hard delete would either lose a student's progress or be
/// permanently blocked once the lesson had ever been recommended.</summary>
public class StudyLesson : ISoftDelete
{
    public int Id { get; set; }
    public required string Title { get; set; }

    public int QuestionAreaId { get; set; }
    public QuestionArea? QuestionArea { get; set; }

    /// <summary>Comma-separated tags consumed by the recommender's lesson-metadata vector.</summary>
    public required string Tags { get; set; }

    public int ValidForYear { get; set; }
    public int ReadingMinutes { get; set; }

    /// <summary>e.g. "Poglavlje 3.1".</summary>
    public required string ChapterLabel { get; set; }

    public ICollection<StudyLessonSection> Sections { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
