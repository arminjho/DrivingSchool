using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Tests;

/// <summary>
/// Student theory-test flow: each test is a "Master" (prijava.md §3.2), questions/answers are
/// the "Details". Retakes are allowed - every /attempts call starts a brand new TestAttempt.
/// </summary>
public class TestAttemptService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TestAttemptService(EAutoskolaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TestSummaryDto>> GetMyTestsAsync(BaseSearchObject search, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;

        var count = await _context.Tests.CountAsync(cancellationToken);

        // Projection: only the columns the list shows, the question count computed by the database.
        var tests = await _context.Tests.AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(t => new TestSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                QuestionAreaName = t.QuestionArea!.Name,
                QuestionCount = t.Questions.Count,
            })
            .ToListAsync(cancellationToken);

        var testIds = tests.Select(t => t.Id).ToList();
        var lastAttempts = await _context.TestAttempts.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.SubmittedAt != null && testIds.Contains(a.TestId))
            .GroupBy(a => a.TestId)
            .Select(g => g.OrderByDescending(a => a.SubmittedAt).First())
            .ToListAsync(cancellationToken);
        var lastAttemptsByTest = lastAttempts.ToDictionary(a => a.TestId);

        foreach (var dto in tests)
        {
            if (lastAttemptsByTest.TryGetValue(dto.Id, out var attempt))
            {
                dto.HasAttempt = true;
                dto.LastPercent = attempt.Percent;
                dto.LastCorrectCount = attempt.CorrectCount;
                dto.LastTotal = attempt.CorrectCount + attempt.IncorrectCount;
                dto.LastPassed = attempt.Passed;
            }
        }

        return new PagedResult<TestSummaryDto> { ResultList = tests, Count = count };
    }

    public async Task<TestAttemptStartDto> StartAttemptAsync(int testId, CancellationToken cancellationToken)
    {
        var test = await _context.Tests
            .Include(t => t.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(t => t.Id == testId, cancellationToken)
            ?? throw new NotFoundException(nameof(Test), testId);

        if (test.Questions.Count == 0)
        {
            throw new BusinessException(Messages.TestHasNoQuestions);
        }

        var attempt = new TestAttempt { StudentId = _currentUser.UserId, TestId = testId };
        _context.TestAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return new TestAttemptStartDto
        {
            AttemptId = attempt.Id,
            TestTitle = test.Title,
            Questions = test.Questions.Select(q => new AttemptQuestionDto
            {
                Id = q.Id, Text = q.Text, ImagePath = q.ImagePath,
                AnswerOptions = q.AnswerOptions.Select(o => new AttemptAnswerOptionDto { Id = o.Id, Text = o.Text }).ToList(),
            }).ToList(),
        };
    }

    public async Task<TestResultDto> SubmitAttemptAsync(int attemptId, SubmitTestAttemptRequest request, CancellationToken cancellationToken)
    {
        var attempt = await _context.TestAttempts
            .Include(a => a.Test).ThenInclude(t => t!.Questions).ThenInclude(q => q.QuestionArea)
            .Include(a => a.Test).ThenInclude(t => t!.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(TestAttempt), attemptId);

        if (attempt.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
        if (attempt.SubmittedAt is not null)
        {
            throw new BusinessException(Messages.AttemptAlreadySubmitted);
        }

        // The last answer sent for a question wins; a duplicate must not crash the request.
        var answersByQuestion = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedAnswerOptionId);
        EnsureAnswersBelongToTest(attempt.Test!, answersByQuestion);

        var weakAreaNames = new HashSet<string>();
        var correctCount = 0;
        var incorrectCount = 0;

        foreach (var question in attempt.Test!.Questions)
        {
            answersByQuestion.TryGetValue(question.Id, out var selectedOptionId);
            var correctOption = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
            var isCorrect = selectedOptionId.HasValue && correctOption is not null && selectedOptionId.Value == correctOption.Id;

            if (isCorrect)
            {
                correctCount++;
            }
            else
            {
                incorrectCount++;
                if (question.QuestionArea is not null)
                {
                    weakAreaNames.Add(question.QuestionArea.Name);
                }
            }

            _context.AttemptAnswers.Add(new AttemptAnswer
            {
                TestAttemptId = attempt.Id, QuestionId = question.Id,
                SelectedAnswerOptionId = selectedOptionId, IsCorrect = isCorrect,
            });
        }

        var total = correctCount + incorrectCount;
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.CorrectCount = correctCount;
        attempt.IncorrectCount = incorrectCount;
        attempt.Percent = total == 0 ? 0 : Math.Round(100.0 * correctCount / total, 1);
        attempt.Passed = attempt.Percent >= attempt.Test.PassThresholdPercent;

        await _context.SaveChangesAsync(cancellationToken);

        return new TestResultDto
        {
            AttemptId = attempt.Id, Percent = attempt.Percent, CorrectCount = correctCount,
            IncorrectCount = incorrectCount, Passed = attempt.Passed, WeakAreas = weakAreaNames.ToList(),
        };
    }

    /// <summary>A selected option must be one of the options of that very question - anything else
    /// (a forged id, an option of another question) is a validation error, not a database failure.</summary>
    private static void EnsureAnswersBelongToTest(Test test, IReadOnlyDictionary<int, int?> answersByQuestion)
    {
        foreach (var (questionId, selectedOptionId) in answersByQuestion)
        {
            var question = test.Questions.FirstOrDefault(q => q.Id == questionId);
            if (question is null || (selectedOptionId.HasValue && question.AnswerOptions.All(o => o.Id != selectedOptionId.Value)))
            {
                throw new ValidationException(nameof(SubmitTestAttemptRequest.Answers), Messages.AnswerOptionInvalid);
            }
        }
    }

    public async Task<TestAttemptReviewDto> GetReviewAsync(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _context.TestAttempts.FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(TestAttempt), attemptId);

        if (attempt.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
        if (attempt.SubmittedAt is null)
        {
            throw new BusinessException(Messages.AttemptReviewNotAvailable);
        }

        var incorrectAnswers = await _context.AttemptAnswers
            .Include(a => a.Question).ThenInclude(q => q!.AnswerOptions)
            .Include(a => a.SelectedAnswerOption)
            .Where(a => a.TestAttemptId == attemptId && !a.IsCorrect)
            .ToListAsync(cancellationToken);

        return new TestAttemptReviewDto
        {
            IncorrectAnswers = incorrectAnswers.Select(a => new TestAttemptReviewItemDto
            {
                QuestionText = a.Question!.Text,
                YourAnswerText = a.SelectedAnswerOption?.Text,
                CorrectAnswerText = a.Question.AnswerOptions.First(o => o.IsCorrect).Text,
            }).ToList(),
        };
    }
}
