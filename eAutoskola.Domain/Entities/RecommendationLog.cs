namespace eAutoskola.Domain.Entities;

/// <summary>Every recommendation actually served to a student is logged here (docs/upute.md
/// §2.4: recommender signals must be genuinely used and stored, not just computed and
/// discarded).</summary>
public class RecommendationLog
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int StudyLessonId { get; set; }
    public StudyLesson? StudyLesson { get; set; }

    public double Score { get; set; }
    public required string Explanation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
