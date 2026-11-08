namespace eAutoskola.Application.Tests;

public class AnswerOptionRequest
{
    /// <summary>Null for a brand new option; set to an existing AnswerOption's Id when editing
    /// a question, so QuestionCrudService.BeforeUpdateAsync updates it in place instead of
    /// recreating it - students may already have answered it (AttemptAnswerConfiguration
    /// restricts deleting an option a student picked).</summary>
    public int? Id { get; set; }
    public required string Text { get; set; }
    public bool IsCorrect { get; set; }
}
