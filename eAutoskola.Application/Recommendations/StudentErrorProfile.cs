namespace eAutoskola.Application.Recommendations;

public record AreaStats(int QuestionAreaId, int TotalAnswered, int IncorrectCount, double ErrorRate, int ErrorFrequency, bool IsCritical);

public class StudentErrorProfile
{
    public Dictionary<int, double> Weights { get; init; } = [];
    public Dictionary<int, AreaStats> AreaStats { get; init; } = [];

    public bool HasAnyData => Weights.Count > 0;
}

/// <summary>One answered question, as needed to build the error profile - a flat, DB-agnostic
/// shape so this can be unit tested without EF Core (see eAutoskola.Tests).</summary>
public record AnsweredQuestionRecord(int QuestionAreaId, int TestAttemptId, bool IsCorrect);

/// <summary>
/// Builds the student's error-profile vector over QuestionArea dimensions, from real stored
/// answers only (docs §6.1). Both ErrorRate AND ErrorFrequency genuinely influence the
/// resulting weight - upute.md §2.4 explicitly forbids collecting a signal and then ignoring
/// it.
///
/// Formula (documented in recommender-dokumentacija.md §5):
///   ErrorRate(area)      = IncorrectCount(area) / TotalAnswered(area)
///   ErrorFrequency(area) = count of DISTINCT attempts with >= 1 error in that area
///   Weight(area)         = ErrorRateWeight * ErrorRate(area)
///                         + ErrorFrequencyWeight * (ErrorFrequency(area) / MaxErrorFrequency)
///   Critical(area)       = ErrorFrequency(area) >= CriticalErrorFrequencyThreshold
/// </summary>
public static class StudentErrorProfileBuilder
{
    public static StudentErrorProfile Build(IReadOnlyCollection<AnsweredQuestionRecord> answeredQuestions)
    {
        var profile = new StudentErrorProfile();
        if (answeredQuestions.Count == 0)
        {
            return profile;
        }

        var byArea = answeredQuestions.GroupBy(a => a.QuestionAreaId);

        var areaRawStats = byArea.Select(g =>
        {
            var total = g.Count();
            var incorrect = g.Count(a => !a.IsCorrect);
            var errorFrequency = g.Where(a => !a.IsCorrect).Select(a => a.TestAttemptId).Distinct().Count();
            var errorRate = total == 0 ? 0 : (double)incorrect / total;
            return (AreaId: g.Key, Total: total, Incorrect: incorrect, ErrorRate: errorRate, ErrorFrequency: errorFrequency);
        }).ToList();

        var maxErrorFrequency = areaRawStats.Count == 0 ? 0 : areaRawStats.Max(s => s.ErrorFrequency);

        foreach (var stat in areaRawStats)
        {
            var normalizedFrequency = maxErrorFrequency == 0 ? 0 : (double)stat.ErrorFrequency / maxErrorFrequency;
            var weight = RecommenderConstants.ErrorRateWeight * stat.ErrorRate
                + RecommenderConstants.ErrorFrequencyWeight * normalizedFrequency;
            var isCritical = stat.ErrorFrequency >= RecommenderConstants.CriticalErrorFrequencyThreshold;

            profile.Weights[stat.AreaId] = weight;
            profile.AreaStats[stat.AreaId] = new AreaStats(stat.AreaId, stat.Total, stat.Incorrect, stat.ErrorRate, stat.ErrorFrequency, isCritical);
        }

        return profile;
    }
}
