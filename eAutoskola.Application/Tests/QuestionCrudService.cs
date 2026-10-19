using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Tests;

public class QuestionCrudService : BaseCRUDService<QuestionDto, QuestionSearchObject, QuestionRequest, QuestionRequest, Question>
{
    public QuestionCrudService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<Question> ApplyFilter(IQueryable<Question> query, QuestionSearchObject search)
    {
        query = query.Include(q => q.QuestionArea).Include(q => q.AnswerOptions);

        if (search.TestId.HasValue)
        {
            query = query.Where(q => q.TestId == search.TestId.Value);
        }
        if (search.QuestionAreaId.HasValue)
        {
            query = query.Where(q => q.QuestionAreaId == search.QuestionAreaId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            query = query.Where(q => q.Text.Contains(search.Text));
        }
        return query;
    }

    protected override IQueryable<Question> GetByIdQuery(int id)
        => Context.Questions.Include(q => q.QuestionArea).Include(q => q.AnswerOptions).Where(q => q.Id == id);

    protected override Task BeforeInsertAsync(Question entity, QuestionRequest request, CancellationToken cancellationToken)
    {
        ValidateAnswerOptions(request.AnswerOptions);
        entity.AnswerOptions = request.AnswerOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect }).ToList();
        return Task.CompletedTask;
    }

    protected override async Task BeforeUpdateAsync(Question entity, QuestionRequest request, CancellationToken cancellationToken)
    {
        ValidateAnswerOptions(request.AnswerOptions);

        var existingOptions = await Context.AnswerOptions.Where(o => o.QuestionId == entity.Id).ToListAsync(cancellationToken);
        Context.AnswerOptions.RemoveRange(existingOptions);

        entity.AnswerOptions = request.AnswerOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect }).ToList();
    }

    protected override async Task BeforeDeleteAsync(Question entity, CancellationToken cancellationToken)
    {
        if (await Context.AttemptAnswers.AnyAsync(a => a.QuestionId == entity.Id, cancellationToken))
        {
            throw new BusinessException(Messages.QuestionHasAnswers);
        }
    }

    private static void ValidateAnswerOptions(List<AnswerOptionRequest> options)
    {
        var errors = new Dictionary<string, string[]>();

        if (options.Count != 4)
        {
            FieldValidationRules.Add(errors, nameof(QuestionRequest.AnswerOptions), Messages.QuestionNeedsFourOptions);
        }
        else if (options.Count(o => o.IsCorrect) != 1)
        {
            FieldValidationRules.Add(errors, nameof(QuestionRequest.AnswerOptions), Messages.QuestionNeedsOneCorrectOption);
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
