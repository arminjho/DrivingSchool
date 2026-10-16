namespace eAutoskola.Application.Tests;

public class TestAttemptReviewItemDto
{
    public required string QuestionText { get; set; }
    public string? YourAnswerText { get; set; }
    public required string CorrectAnswerText { get; set; }
}

/// <summary>Incorrect answers only, with the correct option - available only after submit.</summary>
public class TestAttemptReviewDto
{
    public List<TestAttemptReviewItemDto> IncorrectAnswers { get; set; } = [];
}
