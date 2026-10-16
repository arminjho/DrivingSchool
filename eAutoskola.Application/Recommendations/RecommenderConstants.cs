namespace eAutoskola.Application.Recommendations;

public static class RecommenderConstants
{
    /// <summary>An area is "critical" once the student has made at least one error in it
    /// across this many distinct test attempts (docs §6: "oblast se označava kao kritična").</summary>
    public const int CriticalErrorFrequencyThreshold = 2;

    /// <summary>Weight given to ErrorRate in the combined per-area score.</summary>
    public const double ErrorRateWeight = 0.6;

    /// <summary>Weight given to normalized ErrorFrequency in the combined per-area score.</summary>
    public const double ErrorFrequencyWeight = 0.4;

    /// <summary>Weight given to a StudyLesson's secondary (tag-matched) area association,
    /// relative to its primary QuestionAreaId (weight 1.0).</summary>
    public const double TagAssociationWeight = 0.3;

    public const int DefaultRecommendationCount = 5;
}
