namespace eAutoskola.Application.Tests;

/// <summary>Exactly 4 AnswerOptions, exactly one with IsCorrect = true - validated in
/// QuestionCrudService.</summary>
public class QuestionRequest
{
    public required string Text { get; set; }
    public string? ImagePath { get; set; }
    public int QuestionAreaId { get; set; }
    public int TestId { get; set; }
    public List<AnswerOptionRequest> AnswerOptions { get; set; } = [];
}
