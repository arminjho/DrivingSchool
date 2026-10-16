namespace eAutoskola.Application.Tests;

/// <summary>GET /api/tests/my row - "Nije riješeno" when HasAttempt is false.</summary>
public class TestSummaryDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string QuestionAreaName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public bool HasAttempt { get; set; }
    public double? LastPercent { get; set; }
    public int? LastCorrectCount { get; set; }
    public int? LastTotal { get; set; }
    public bool? LastPassed { get; set; }
}
