namespace eAutoskola.Application.Tests;

public class AttemptAnswerOptionDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
}

public class AttemptQuestionDto
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public string? ImagePath { get; set; }
    public List<AttemptAnswerOptionDto> AnswerOptions { get; set; } = [];
}

/// <summary>Questions are sent WITHOUT IsCorrect flags (prompt 2's explicit requirement).</summary>
public class TestAttemptStartDto
{
    public int AttemptId { get; set; }
    public required string TestTitle { get; set; }
    public List<AttemptQuestionDto> Questions { get; set; } = [];
}
