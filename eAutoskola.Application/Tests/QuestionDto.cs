namespace eAutoskola.Application.Tests;

public class QuestionDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public string? ImagePath { get; set; }
    public int QuestionAreaId { get; set; }
    public string QuestionAreaName { get; set; } = string.Empty;
    public int TestId { get; set; }
    public List<AnswerOptionDto> AnswerOptions { get; set; } = [];
}
