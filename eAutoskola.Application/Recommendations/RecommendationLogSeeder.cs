using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Recommendations;

/// <summary>
/// Fills RecommendationLogs of a freshly seeded database by running the real recommender for a few
/// students who have solved tests - the scores and explanations are the recommender's own output
/// for the seeded answers, not typed-in sample values. Does nothing once any log exists, so a
/// restart never adds to it.
/// </summary>
public static class RecommendationLogSeeder
{
    private const int StudentCount = 6;

    public static async Task SeedAsync(EAutoskolaDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.RecommendationLogs.AnyAsync(cancellationToken))
        {
            return;
        }

        var studentIds = await context.TestAttempts
            .Where(a => a.SubmittedAt != null)
            .Select(a => a.StudentId)
            .Distinct()
            .OrderBy(id => id)
            .Take(StudentCount)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentIds)
        {
            var service = new StudyRecommendationService(context, new SeedingStudent(studentId));
            await service.GetRecommendationsAsync(RecommenderConstants.DefaultRecommendationCount, cancellationToken);
        }
    }

    /// <summary>The student the recommender is asked about; there is no HTTP request while seeding.</summary>
    private sealed class SeedingStudent(int userId) : ICurrentUserService
    {
        public int UserId => userId;
        public string Role => Domain.Security.Role.Student;
        public bool IsInRole(string role) => role == Domain.Security.Role.Student;
    }
}
