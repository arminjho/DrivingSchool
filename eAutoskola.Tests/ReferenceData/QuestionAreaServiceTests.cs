using eAutoskola.Application.ReferenceData.QuestionAreas;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#11: QuestionAreaService had no validation and no delete-reason message -
/// a question area can be used by Questions, Tests and StudyLessons, each checked in turn.</summary>
public class QuestionAreaServiceTests
{
    static QuestionAreaServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(QuestionAreaService).Assembly);

    private static (QuestionAreaService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.QuestionAreas.Add(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" });
        context.SaveChanges();
        return (new QuestionAreaService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankName_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new QuestionAreaRequest { Name = "" }));
    }

    [Fact]
    public async Task Delete_AnAreaUsedByAQuestion_IsRejectedWithTheQuestionCount()
    {
        var (service, context) = Create();
        context.Tests.Add(new Test { Id = 1, Title = "Test 1", QuestionAreaId = 1 });
        context.Questions.Add(new Question { Id = 1, Text = "Pitanje?", QuestionAreaId = 1, TestId = 1 });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_AnAreaUsedOnlyByAStudyLesson_IsRejectedWithTheLessonCount()
    {
        var (service, context) = Create();
        context.StudyLessons.Add(new StudyLesson { Id = 1, Title = "Lekcija", QuestionAreaId = 1, Tags = "znakovi", ChapterLabel = "1.1" });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
    }

    [Fact]
    public async Task Delete_AnUnusedArea_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.QuestionAreas.CountAsync());
    }
}
