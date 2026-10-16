namespace eAutoskola.Application.Tests;

public class TestRequest
{
    public required string Title { get; set; }
    public int QuestionAreaId { get; set; }
    public int PassThresholdPercent { get; set; } = 70;
}
