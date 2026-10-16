namespace eAutoskola.Application.Recommendations;

/// <summary>
/// Generates the Bosnian explanation text shown to the student, built entirely from real
/// numbers (never a canned/generic sentence) - docs §6.3 "Explainable AI".
/// </summary>
public static class ExplanationGenerator
{
    /// <param name="lastAttemptAreaErrorPercent">The student's error percentage WITHIN this
    /// specific area, computed from their most recent attempt that included questions from
    /// it - not the lifetime aggregate ErrorRate, so the sentence reads naturally ("na
    /// posljednjem probnom testu").</param>
    public static string Generate(string studyLessonTitle, string areaName, int lastAttemptAreaErrorPercent, bool isCritical)
    {
        var text = $"Preporučujemo vam da ponovite lekciju '{studyLessonTitle}' jer ste na posljednjem probnom testu " +
                   $"imali {lastAttemptAreaErrorPercent}% netačnih odgovora iz ove specifične kategorije ({areaName}).";

        if (isCritical)
        {
            text += " Ova oblast je označena kao kritična jer ste greške iz nje ponovili u više probnih testova.";
        }

        return text;
    }

    /// <summary>Header text shown above the recommendations list on the student's screen
    /// (docs/prijava.md §4.2.8).</summary>
    public static string ColdStartMessage() =>
        "Preporuke će biti dostupne nakon što riješite barem jedan probni test.";
}
