using eAutoskola.Application.Tests;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Theory;

/// <summary>The admin "Pitanja" table: every control on it is backed by a server-side filter.</summary>
public class QuestionCrudServiceTests
{
    static QuestionCrudServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(QuestionCrudService).Assembly);

    private static QuestionCrudService CreateWithQuestions()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.QuestionAreas.AddRange(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" }, new QuestionArea { Id = 2, Name = "Pravila vožnje" });
        context.Tests.AddRange(new Test { Id = 1, Title = "Test 1", QuestionAreaId = 1 }, new Test { Id = 2, Title = "Test 2", QuestionAreaId = 2 });
        context.Questions.AddRange(
            new Question { Id = 1, Text = "Koji znak označava obavezno skretanje udesno?", QuestionAreaId = 1, TestId = 1 },
            new Question { Id = 2, Text = "Šta znači znak Stop?", QuestionAreaId = 1, TestId = 1 },
            new Question { Id = 3, Text = "Ko ima prvenstvo prolaza na raskrsnici?", QuestionAreaId = 2, TestId = 2 });
        context.SaveChanges();
        return new QuestionCrudService(context);
    }

    [Fact]
    public async Task SearchText_FindsQuestionsContainingIt()
    {
        var service = CreateWithQuestions();

        var page = await service.GetPagedAsync(new QuestionSearchObject { Text = "znak" }, default);

        Assert.Equal(2, page.Count);
        Assert.All(page.ResultList, q => Assert.Contains("znak", q.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchText_CombinesWithTheTestFilter_AndBlankTextMeansNoFilter()
    {
        var service = CreateWithQuestions();

        Assert.Equal(1, (await service.GetPagedAsync(new QuestionSearchObject { TestId = 1, Text = "Stop" }, default)).Count);
        Assert.Equal(0, (await service.GetPagedAsync(new QuestionSearchObject { TestId = 2, Text = "Stop" }, default)).Count);
        Assert.Equal(3, (await service.GetPagedAsync(new QuestionSearchObject { Text = "  " }, default)).Count);
    }
}
