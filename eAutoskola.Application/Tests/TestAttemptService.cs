using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
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

    public async Task<List<TestSummaryDto>> GetMyTestsAsync(CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;

        var tests = await _context.Tests
            .Include(t => t.QuestionArea)
            .Include(t => t.Questions)
            .ToListAsync(cancellationToken);

        var lastAttempts = await _context.TestAttempts
            .Where(a => a.StudentId == studentId && a.SubmittedAt != null)
            .GroupBy(a => a.TestId)
            .Select(g => g.OrderByDescending(a => a.SubmittedAt).First())
            .ToListAsync(cancellationToken);
        var lastAttemptsByTest = lastAttempts.ToDictionary(a => a.TestId);

        return tests.Select(t =>
        {
            var dto = new TestSummaryDto
            {
                Id = t.Id, Title = t.Title, QuestionAreaName = t.QuestionArea?.Name ?? string.Empty,
                QuestionCount = t.Questions.Count,
            };

            if (lastAttemptsByTest.TryGetValue(t.Id, out var attempt))
            {
                dto.HasAttempt = true;
                dto.LastPercent = attempt.Percent;
                dto.LastCorrectCount = attempt.CorrectCount;
                dto.LastTotal = attempt.CorrectCount + attempt.IncorrectCount;
                dto.LastPassed = attempt.Passed;
            }

            return dto;
        }).ToList();
    }

    public async Task<TestAttemptStartDto> StartAttemptAsync(int testId, CancellationToken cancellationToken)
    {
        var test = await _context.Tests
            .Include(t => t.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(t => t.Id == testId, cancellationToken)
            ?? throw new NotFoundException(nameof(Test), testId);

        if (test.Questions.Count == 0)
        {
            throw new BusinessException("Test trenutno nema pitanja.");
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
            .Include(a => a.Test).ThenInclude(t => t!.Questions).ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken)
            ?? throw new NotFoundException(nameof(TestAttempt), attemptId);

        if (attempt.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
        if (attempt.SubmittedAt is not null)
        {
            throw new BusinessException("Ovaj pokušaj je već predat.");
        }

        var answersByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedAnswerOptionId);
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
                var area = await _context.QuestionAreas.FirstOrDefaultAsync(a => a.Id == question.QuestionAreaId, cancellationToken);
                if (area is not null)
                {
                    weakAreaNames.Add(area.Name);
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
            throw new BusinessException("Pregled je dostupan tek nakon predaje testa.");
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
