namespace eAutoskola.Application.Tests;

public class TestResultDto
{
    public int AttemptId { get; set; }
    public double Percent { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public bool Passed { get; set; }
    public List<string> WeakAreas { get; set; } = [];
}
