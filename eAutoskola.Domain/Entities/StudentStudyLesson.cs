namespace eAutoskola.Domain.Entities;

/// <summary>"Moje lekcije" progress tracking - a row exists once a student has opened a given
/// StudyLesson at least once.</summary>
public class StudentStudyLesson
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int StudyLessonId { get; set; }
    public StudyLesson? StudyLesson { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastOpenedAt { get; set; } = DateTime.UtcNow;
    public int ProgressPercent { get; set; }
}
