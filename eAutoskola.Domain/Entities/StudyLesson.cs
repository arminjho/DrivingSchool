namespace eAutoskola.Domain.Entities;

public class StudyLesson
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
}
