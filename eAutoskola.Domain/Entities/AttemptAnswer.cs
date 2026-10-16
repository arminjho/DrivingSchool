namespace eAutoskola.Domain.Entities;

public class AttemptAnswer
{
    public int Id { get; set; }

    public int TestAttemptId { get; set; }
    public TestAttempt? TestAttempt { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    /// <summary>Null if the student left the question unanswered.</summary>
    public int? SelectedAnswerOptionId { get; set; }
    public AnswerOption? SelectedAnswerOption { get; set; }

    public bool IsCorrect { get; set; }
}
