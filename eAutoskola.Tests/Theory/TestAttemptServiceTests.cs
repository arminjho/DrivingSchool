using eAutoskola.Application.Common;
using eAutoskola.Application.Tests;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Tests.Payments;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Theory;

/// <summary>Submitting a theory test: scoring, weak areas, and a request that cannot crash it.</summary>
public class TestAttemptServiceTests
{
    private sealed record Env(PaymentTestFixture F, TestAttemptService Service);

    private static Env Create()
    {
        var f = new PaymentTestFixture();
        f.Context.QuestionAreas.AddRange(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" }, new QuestionArea { Id = 2, Name = "Pravila vožnje" });

        static Question NewQuestion(int id, int areaId) => new()
        {
            Id = id, Text = $"Pitanje {id}", QuestionAreaId = areaId, TestId = 1,
            AnswerOptions = Enumerable.Range(0, 4).Select(i => new AnswerOption { Id = id * 10 + i, Text = $"Opcija {i}", IsCorrect = i == 1 }).ToList(),
        };

        f.Context.Tests.Add(new Test
        {
            Id = 1, Title = "Test 1", QuestionAreaId = 1, PassThresholdPercent = 50,
            Questions = [NewQuestion(1, 1), NewQuestion(2, 2), NewQuestion(3, 2)],
        });
        f.Context.TestAttempts.Add(new TestAttempt { Id = 1, StudentId = PaymentTestFixture.StudentId, TestId = 1 });
        f.Context.SaveChanges();

        return new Env(f, new TestAttemptService(f.Context, f.CurrentUser.Object));
    }

    private static SubmitTestAttemptRequest Answers(params (int Question, int? Option)[] answers)
        => new() { Answers = answers.Select(a => new SubmittedAnswer { QuestionId = a.Question, SelectedAnswerOptionId = a.Option }).ToList() };

    [Fact]
    public async Task Submit_ScoresTheAttemptAndNamesTheAreasWithMistakes()
    {
        var env = Create();

        // question 1 right (option index 1 = id 11), question 2 wrong (id 20), question 3 not answered
        var result = await env.Service.SubmitAttemptAsync(1, Answers((1, 11), (2, 20)), default);

        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(2, result.IncorrectCount);
        Assert.Equal(33.3, result.Percent);
        Assert.False(result.Passed);
        Assert.Equal(["Pravila vožnje"], result.WeakAreas);
        var stored = await env.F.Context.AttemptAnswers.Where(a => a.TestAttemptId == 1).ToListAsync();
        Assert.Equal(3, stored.Count);
        Assert.Single(stored, a => a.IsCorrect);
    }

    [Fact]
    public async Task Submit_AnOptionThatBelongsToAnotherQuestion_IsAValidationErrorNotAServerError()
    {
        var env = Create();

        await Assert.ThrowsAsync<ValidationException>(() => env.Service.SubmitAttemptAsync(1, Answers((1, 21)), default));
        await Assert.ThrowsAsync<ValidationException>(() => env.Service.SubmitAttemptAsync(1, Answers((99, 11)), default));
    }

    [Fact]
    public async Task Submit_TheSameQuestionTwice_TheLastAnswerCounts()
    {
        var env = Create();

        var result = await env.Service.SubmitAttemptAsync(1, Answers((1, 10), (1, 11), (2, 21), (3, 31)), default);

        Assert.Equal(3, result.CorrectCount);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Submit_TwiceOrForSomeoneElsesAttempt_IsRefused()
    {
        var env = Create();
        await env.Service.SubmitAttemptAsync(1, Answers((1, 11)), default);

        await Assert.ThrowsAsync<BusinessException>(() => env.Service.SubmitAttemptAsync(1, Answers((1, 11)), default));

        env.F.Context.TestAttempts.Add(new TestAttempt { Id = 2, StudentId = PaymentTestFixture.OtherStudentId, TestId = 1 });
        await env.F.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => env.Service.SubmitAttemptAsync(2, Answers((1, 11)), default));
    }

    [Fact]
    public async Task Review_ListsOnlyTheIncorrectAnswersAfterSubmitting()
    {
        var env = Create();
        await Assert.ThrowsAsync<BusinessException>(() => env.Service.GetReviewAsync(1, default));
        await env.Service.SubmitAttemptAsync(1, Answers((1, 11), (2, 20), (3, 31)), default);

        var review = await env.Service.GetReviewAsync(1, default);

        var item = Assert.Single(review.IncorrectAnswers);
        Assert.Equal("Pitanje 2", item.QuestionText);
        Assert.Equal("Opcija 1", item.CorrectAnswerText);
    }

    [Fact]
    public async Task MyTests_ArePaged_ShowTheLastResultAndTheQuestionCount()
    {
        var env = Create();
        await env.Service.SubmitAttemptAsync(1, Answers((1, 11), (2, 21), (3, 31)), default);
        env.F.Context.Tests.Add(new Test { Id = 2, Title = "Test 2", QuestionAreaId = 2 });
        await env.F.Context.SaveChangesAsync();

        var page = await env.Service.GetMyTestsAsync(new PageQuery { PageSize = 1 }, default);

        Assert.Equal(2, page.Count);
        var first = Assert.Single(page.ResultList);
        Assert.Equal("Test 1", first.Title);
        Assert.True(first.HasAttempt);
        Assert.Equal(100, first.LastPercent);
        Assert.Equal(3, first.QuestionCount);

        var second = Assert.Single((await env.Service.GetMyTestsAsync(new PageQuery { Page = 2, PageSize = 1 }, default)).ResultList);
        Assert.False(second.HasAttempt);
    }
}
