using eAutoskola.Application.Recommendations;

namespace eAutoskola.Tests.Recommendations;

public class ExplanationGeneratorTests
{
    [Fact]
    public void Generate_IncludesLessonTitleAndPercentAndArea()
    {
        var text = ExplanationGenerator.Generate("Prvenstvo prolaza na raskrsnicama", "Saobraćajni znakovi", 75, isCritical: false);

        Assert.Contains("Prvenstvo prolaza na raskrsnicama", text);
        Assert.Contains("75%", text);
        Assert.Contains("Saobraćajni znakovi", text);
    }

    [Fact]
    public void Generate_NotCritical_DoesNotMentionCriticalArea()
    {
        var text = ExplanationGenerator.Generate("Lekcija", "Oblast", 50, isCritical: false);

        Assert.DoesNotContain("kritična", text);
    }

    [Fact]
    public void Generate_Critical_MentionsCriticalArea()
    {
        var text = ExplanationGenerator.Generate("Lekcija", "Oblast", 50, isCritical: true);

        Assert.Contains("kritična", text);
    }

    [Fact]
    public void ColdStartMessage_IsNonEmptyBosnianText()
    {
        var message = ExplanationGenerator.ColdStartMessage();

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.Contains("probni test", message, StringComparison.OrdinalIgnoreCase);
    }
}
