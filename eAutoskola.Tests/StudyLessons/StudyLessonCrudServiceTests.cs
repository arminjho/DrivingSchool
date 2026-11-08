using eAutoskola.Application.StudyLessons;
using eAutoskola.Domain.Entities;
using eAutoskola.Tests.Payments;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.StudyLessons;

/// <summary>Dorada #11: a StudyLesson that was ever recommended could never be deleted -
/// RecommendationLogConfiguration restricts deleting its StudyLesson FK, and
/// StudyRecommendationService logs one every time the recommendations screen is opened, so the
/// admin got the generic "record in use" message with no way to ever remove the lesson. It is
/// now soft-deleted, like Vehicle/User, so deletion always succeeds.</summary>
public class StudyLessonCrudServiceTests
{
    static StudyLessonCrudServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(StudyLessonCrudService).Assembly);

    private static (StudyLessonCrudService Service, PaymentTestFixture F, StudyLesson Lesson) Create()
    {
        var f = new PaymentTestFixture();
        f.Context.QuestionAreas.Add(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" });
        var lesson = new StudyLesson { Id = 1, Title = "Znakovi opasnosti", QuestionAreaId = 1, Tags = "znakovi,opasnost", ChapterLabel = "1.1" };
        f.Context.StudyLessons.Add(lesson);
        f.Context.SaveChanges();
        return (new StudyLessonCrudService(f.Context, f.CurrentUser.Object), f, lesson);
    }

    [Fact]
    public async Task Delete_ALessonThatWasRecommended_StillSucceeds_ViaSoftDelete()
    {
        var (service, f, lesson) = Create();
        f.Context.RecommendationLogs.Add(new RecommendationLog { StudentId = PaymentTestFixture.StudentId, StudyLessonId = lesson.Id, Score = 0.5, Explanation = "x" });
        await f.Context.SaveChangesAsync();

        await service.DeleteAsync(lesson.Id);

        var stored = await f.Context.StudyLessons.IgnoreQueryFilters().AsNoTracking().SingleAsync(l => l.Id == lesson.Id);
        Assert.True(stored.IsDeleted);
        Assert.NotNull(stored.DeletedAt);
        // The recommendation log itself is untouched - soft delete never cascades.
        Assert.Equal(1, await f.Context.RecommendationLogs.CountAsync());
    }

    [Fact]
    public async Task Delete_ALessonThatAStudentHasOpened_StillSucceeds_ViaSoftDelete()
    {
        var (service, f, lesson) = Create();
        f.Context.StudentStudyLessons.Add(new StudentStudyLesson
        {
            StudentId = PaymentTestFixture.StudentId, StudyLessonId = lesson.Id,
            OpenedAt = DateTime.UtcNow, LastOpenedAt = DateTime.UtcNow, ProgressPercent = 100,
        });
        await f.Context.SaveChangesAsync();

        await service.DeleteAsync(lesson.Id);

        var stored = await f.Context.StudyLessons.IgnoreQueryFilters().AsNoTracking().SingleAsync(l => l.Id == lesson.Id);
        Assert.True(stored.IsDeleted);
    }

    [Fact]
    public async Task ASoftDeletedLesson_NoLongerAppearsInTheList_OrByGetById()
    {
        var (service, f, lesson) = Create();

        await service.DeleteAsync(lesson.Id);

        var page = await service.GetPagedAsync(new StudyLessonSearchObject());
        Assert.Empty(page.ResultList);
        Assert.Null(await service.GetByIdAsync(lesson.Id));
    }
}
