namespace eAutoskola.Application.Recommendations;

public class RecommendationDto
{
    public int StudyLessonId { get; set; }
    public required string Title { get; set; }
    public required string ChapterLabel { get; set; }
    public int ReadingMinutes { get; set; }
    public double Score { get; set; }
    public required string Explanation { get; set; }
    public bool IsCriticalArea { get; set; }
}

public class RecommendationsResultDto
{
    public List<RecommendationDto> Recommendations { get; set; } = [];

    /// <summary>Set only when there is no data yet (cold start) - the client shows this
    /// instead of an empty list.</summary>
    public string? ColdStartMessage { get; set; }
}
