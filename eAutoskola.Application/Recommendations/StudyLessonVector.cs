namespace eAutoskola.Application.Recommendations;

/// <summary>DB-agnostic shape for one StudyLesson's area/tags, as needed to build its metadata
/// vector - unit-testable without EF Core.</summary>
public record StudyLessonMetadata(int StudyLessonId, int PrimaryQuestionAreaId, string Tags);

/// <summary>
/// Builds a StudyLesson's metadata vector over the same QuestionArea dimensions as the
/// student's error profile, so cosine similarity between the two is meaningful (docs §6.2).
///
/// Formula (documented in recommender-dokumentacija.md §4):
///   vector[PrimaryQuestionAreaId] = 1.0
///   vector[otherAreaId] = TagAssociationWeight, for every OTHER area whose name appears as a
///                         substring of the lesson's Tags (case-insensitive) - this is what
///                         makes the stored Tags field genuinely influence the vector, not just
///                         the single QuestionAreaId column.
/// </summary>
public static class StudyLessonVectorBuilder
{
    public static Dictionary<int, double> Build(StudyLessonMetadata lesson, IReadOnlyDictionary<int, string> allQuestionAreaNames)
    {
        var vector = new Dictionary<int, double> { [lesson.PrimaryQuestionAreaId] = 1.0 };

        var tagsLower = lesson.Tags.ToLowerInvariant();
        foreach (var (areaId, areaName) in allQuestionAreaNames)
        {
            if (areaId == lesson.PrimaryQuestionAreaId)
            {
                continue;
            }

            if (tagsLower.Contains(areaName.ToLowerInvariant()))
            {
                vector[areaId] = RecommenderConstants.TagAssociationWeight;
            }
        }

        return vector;
    }
}
