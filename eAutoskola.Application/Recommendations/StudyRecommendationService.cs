using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Recommendations;

/// <summary>Typed projection of one answered question, used instead of an anonymous type so no
/// `dynamic` is ever needed downstream (upute.md §3.4 explicitly bans dynamic).</summary>
internal record AnsweredQuestionRow(int QuestionAreaId, int TestAttemptId, bool IsCorrect, DateTime SubmittedAt);

/// <summary>
/// Orchestrates the recommender: pulls real stored data (never synthetic), delegates to the
/// pure/testable builders in this namespace, ranks by cosine similarity, and logs every served
/// recommendation (upute.md §2.4). See recommender-dokumentacija.md for the full writeup -
/// this class and that document must stay in sync.
/// </summary>
public class StudyRecommendationService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public StudyRecommendationService(EAutoskolaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<RecommendationsResultDto> GetRecommendationsAsync(int take, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;

        // One query, all answered questions across every submitted attempt - avoids N+1 and
        // feeds both the error profile AND the "last attempt per area" breakdown below.
        var answered = await _context.AttemptAnswers
            .Where(a => a.TestAttempt!.StudentId == studentId && a.TestAttempt.SubmittedAt != null)
            .Select(a => new AnsweredQuestionRow(a.Question!.QuestionAreaId, a.TestAttemptId, a.IsCorrect, a.TestAttempt!.SubmittedAt!.Value))
            .ToListAsync(cancellationToken);

        if (answered.Count == 0)
        {
            return new RecommendationsResultDto { ColdStartMessage = ExplanationGenerator.ColdStartMessage() };
        }

        var profile = StudentErrorProfileBuilder.Build(
            answered.Select(a => new AnsweredQuestionRecord(a.QuestionAreaId, a.TestAttemptId, a.IsCorrect)).ToList());

        if (!profile.HasAnyData)
        {
            return new RecommendationsResultDto { ColdStartMessage = ExplanationGenerator.ColdStartMessage() };
        }

        var questionAreas = await _context.QuestionAreas.AsNoTracking().ToListAsync(cancellationToken);
        var areaNames = questionAreas.ToDictionary(a => a.Id, a => a.Name);

        var studyLessons = await _context.StudyLessons.AsNoTracking().ToListAsync(cancellationToken);

        var scored = studyLessons
            .Select(lesson =>
            {
                var vector = StudyLessonVectorBuilder.Build(new StudyLessonMetadata(lesson.Id, lesson.QuestionAreaId, lesson.Tags), areaNames);
                var score = VectorMath.CosineSimilarity(profile.Weights, vector);
                return (Lesson: lesson, Score: score);
            })
            .Where(x => x.Score > 0) // exclude zero-similarity lessons
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Lesson.Id) // deterministic tie-break
            .Take(take)
            .ToList();

        var recommendations = new List<RecommendationDto>();
        var logs = new List<RecommendationLog>();

        foreach (var (lesson, score) in scored)
        {
            var areaName = areaNames.GetValueOrDefault(lesson.QuestionAreaId, string.Empty);
            var isCritical = profile.AreaStats.TryGetValue(lesson.QuestionAreaId, out var stats) && stats.IsCritical;
            var lastAttemptPercent = ComputeLastAttemptAreaErrorPercent(answered, lesson.QuestionAreaId);

            var explanation = ExplanationGenerator.Generate(lesson.Title, areaName, lastAttemptPercent, isCritical);

            recommendations.Add(new RecommendationDto
            {
                StudyLessonId = lesson.Id, Title = lesson.Title, ChapterLabel = lesson.ChapterLabel,
                ReadingMinutes = lesson.ReadingMinutes, Score = Math.Round(score, 4),
                Explanation = explanation, IsCriticalArea = isCritical,
            });

            logs.Add(new RecommendationLog
            {
                StudentId = studentId, StudyLessonId = lesson.Id, Score = score, Explanation = explanation,
            });
        }

        if (logs.Count > 0)
        {
            _context.RecommendationLogs.AddRange(logs);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new RecommendationsResultDto { Recommendations = recommendations };
    }

    /// <summary>Error percentage within one area, from the single most recent submitted
    /// attempt that included questions from it - not the lifetime aggregate.</summary>
    private static int ComputeLastAttemptAreaErrorPercent(List<AnsweredQuestionRow> answered, int questionAreaId)
    {
        var inArea = answered
            .Where(a => a.QuestionAreaId == questionAreaId)
            .GroupBy(a => a.TestAttemptId)
            .Select(g => new
            {
                SubmittedAt = g.First().SubmittedAt,
                Total = g.Count(),
                Incorrect = g.Count(a => !a.IsCorrect),
            })
            .OrderByDescending(g => g.SubmittedAt)
            .FirstOrDefault();

        if (inArea is null || inArea.Total == 0)
        {
            return 0;
        }

        return (int)Math.Round(100.0 * inArea.Incorrect / inArea.Total);
    }
}
