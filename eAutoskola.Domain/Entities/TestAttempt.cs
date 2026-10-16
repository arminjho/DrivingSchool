namespace eAutoskola.Domain.Entities;

/// <summary>RezultatTesta in the Bosnian plan - glossary name is TestAttempt
/// (CLAUDE.md §3).</summary>
public class TestAttempt
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int TestId { get; set; }
    public Test? Test { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public double Percent { get; set; }
    public bool Passed { get; set; }

    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}
