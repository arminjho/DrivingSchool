using eAutoskola.Application.Tests;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
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

    // --- Dorada #8: editing a question a student already answered must not touch the answered option's row ---

    private static (QuestionCrudService Service, EAutoskolaDbContext Context, Question Question) CreateWithFourOptions()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.QuestionAreas.Add(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" });
        context.Tests.Add(new Test { Id = 1, Title = "Test 1", QuestionAreaId = 1 });
        var question = new Question { Id = 1, Text = "Šta znači znak Stop?", QuestionAreaId = 1, TestId = 1 };
        context.Questions.Add(question);
        context.AnswerOptions.AddRange(
            new AnswerOption { Id = 1, QuestionId = 1, Text = "A", IsCorrect = false },
            new AnswerOption { Id = 2, QuestionId = 1, Text = "B", IsCorrect = true },
            new AnswerOption { Id = 3, QuestionId = 1, Text = "C", IsCorrect = false },
            new AnswerOption { Id = 4, QuestionId = 1, Text = "D", IsCorrect = false });
        context.Users.Add(new User { Id = 1, Email = "s@test.ba", PasswordHash = "x", Role = Role.Student, FirstName = "S", LastName = "T", PhoneNumber = "+387 61 000 001" });
        context.TestAttempts.Add(new TestAttempt { Id = 1, StudentId = 1, TestId = 1, Answers = [new AttemptAnswer { QuestionId = 1, SelectedAnswerOptionId = 2, IsCorrect = true }] });
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return (new QuestionCrudService(context), context, question);
    }

    private static QuestionRequest UpdateRequest(params AnswerOptionRequest[] options) => new()
    {
        Text = "Šta znači znak Stop (izmijenjeno)?", QuestionAreaId = 1, TestId = 1, AnswerOptions = options.ToList(),
    };

    [Fact]
    public async Task Update_AQuestionAStudentAlreadyAnswered_UpdatesTheAnsweredOptionInPlace_InsteadOfRecreatingIt()
    {
        var (service, context, question) = CreateWithFourOptions();

        await service.UpdateAsync(question.Id, UpdateRequest(
            new AnswerOptionRequest { Id = 1, Text = "A izmijenjeno", IsCorrect = false },
            new AnswerOptionRequest { Id = 2, Text = "B izmijenjeno", IsCorrect = true },
            new AnswerOptionRequest { Id = 3, Text = "C izmijenjeno", IsCorrect = false },
            new AnswerOptionRequest { Id = 4, Text = "D izmijenjeno", IsCorrect = false }), default);

        var options = await context.AnswerOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
        Assert.Equal(4, options.Count);
        Assert.Equal([1, 2, 3, 4], options.Select(o => o.Id).Order());
        Assert.Equal("B izmijenjeno", options.Single(o => o.Id == 2).Text);

        // The student's already-recorded answer still points at the very same (now-updated) row.
        var attemptAnswer = await context.AttemptAnswers.SingleAsync();
        Assert.Equal(2, attemptAnswer.SelectedAnswerOptionId);
    }

    [Fact]
    public async Task Update_ReplacingAnUnansweredOption_RemovesTheOldOneAndCreatesANewRow()
    {
        var (service, context, question) = CreateWithFourOptions();

        await service.UpdateAsync(question.Id, UpdateRequest(
            new AnswerOptionRequest { Id = 1, Text = "A", IsCorrect = false },
            new AnswerOptionRequest { Id = 2, Text = "B", IsCorrect = true },
            new AnswerOptionRequest { Id = 3, Text = "C", IsCorrect = false },
            new AnswerOptionRequest { Text = "Nova opcija D", IsCorrect = false }), default); // option 4 replaced, no Id

        var options = await context.AnswerOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
        Assert.Equal(4, options.Count);
        Assert.DoesNotContain(options, o => o.Id == 4);
        Assert.Contains(options, o => o.Text == "Nova opcija D");
    }

    [Fact]
    public async Task Update_StillRequiresExactlyFourOptionsWithOneCorrect()
    {
        var (service, _, question) = CreateWithFourOptions();

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(question.Id, UpdateRequest(
            new AnswerOptionRequest { Id = 1, Text = "A", IsCorrect = false },
            new AnswerOptionRequest { Id = 2, Text = "B", IsCorrect = false },
            new AnswerOptionRequest { Id = 3, Text = "C", IsCorrect = false },
            new AnswerOptionRequest { Id = 4, Text = "D", IsCorrect = false }), default));
    }

    // --- Dorada #10: blank question/answer text used to be accepted silently ---

    [Fact]
    public async Task Update_WithABlankQuestionText_IsRejected()
    {
        var (service, _, question) = CreateWithFourOptions();

        var request = UpdateRequest(
            new AnswerOptionRequest { Id = 1, Text = "A", IsCorrect = false },
            new AnswerOptionRequest { Id = 2, Text = "B", IsCorrect = true },
            new AnswerOptionRequest { Id = 3, Text = "C", IsCorrect = false },
            new AnswerOptionRequest { Id = 4, Text = "D", IsCorrect = false });
        request.Text = "   ";

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(question.Id, request, default));
    }

    [Fact]
    public async Task Update_WithABlankAnswerOptionText_IsRejected()
    {
        var (service, _, question) = CreateWithFourOptions();

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(question.Id, UpdateRequest(
            new AnswerOptionRequest { Id = 1, Text = "", IsCorrect = false },
            new AnswerOptionRequest { Id = 2, Text = "B", IsCorrect = true },
            new AnswerOptionRequest { Id = 3, Text = "C", IsCorrect = false },
            new AnswerOptionRequest { Id = 4, Text = "D", IsCorrect = false }), default));
    }

    [Fact]
    public async Task Insert_WithABlankQuestionText_IsRejected()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.QuestionAreas.Add(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" });
        context.Tests.Add(new Test { Id = 1, Title = "Test 1", QuestionAreaId = 1 });
        context.SaveChanges();
        var service = new QuestionCrudService(context);

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new QuestionRequest
        {
            Text = " ", QuestionAreaId = 1, TestId = 1,
            AnswerOptions =
            [
                new AnswerOptionRequest { Text = "A", IsCorrect = false },
                new AnswerOptionRequest { Text = "B", IsCorrect = true },
                new AnswerOptionRequest { Text = "C", IsCorrect = false },
                new AnswerOptionRequest { Text = "D", IsCorrect = false },
            ],
        }, default));
    }
}
