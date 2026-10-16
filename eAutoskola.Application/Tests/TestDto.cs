namespace eAutoskola.Application.Tests;

public class TestDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int QuestionAreaId { get; set; }
    public string QuestionAreaName { get; set; } = string.Empty;
    public int PassThresholdPercent { get; set; }
    public int QuestionCount { get; set; }
}
