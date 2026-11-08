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
        ValidateQuestion(request);
        entity.AnswerOptions = request.AnswerOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect }).ToList();
        return Task.CompletedTask;
    }

    /// <summary>Updates existing AnswerOptions in place (matched by Id) instead of deleting and
    /// recreating all four - once a student has answered a question, AttemptAnswerConfiguration
    /// restricts deleting the option they picked, so recreating it broke every later edit, even
    /// a text/image fix, with a generic "record in use" error. A request option only becomes a
    /// new row when it carries no Id, or an Id that no longer matches an existing option; an
    /// existing option no longer referenced by the request is removed exactly as before.</summary>
    protected override async Task BeforeUpdateAsync(Question entity, QuestionRequest request, CancellationToken cancellationToken)
    {
        ValidateQuestion(request);

        // request.Adapt(entity) (CrudOperations.UpdateAsync, before this hook runs) auto-maps
        // AnswerOptionRequest's Id/Text/IsCorrect onto a brand new, untracked AnswerOptions list
        // by member name - discard it before tracking the real rows below, or EF's change
        // tracker finds two different instances claiming the same AnswerOption.Id and throws.
        entity.AnswerOptions = [];

        var existingOptions = await Context.AnswerOptions.Where(o => o.QuestionId == entity.Id).ToListAsync(cancellationToken);
        var existingById = existingOptions.ToDictionary(o => o.Id);
        var keptIds = new HashSet<int>();

        foreach (var optionRequest in request.AnswerOptions)
        {
            if (optionRequest.Id is int id && existingById.TryGetValue(id, out var existing))
            {
                existing.Text = optionRequest.Text;
                existing.IsCorrect = optionRequest.IsCorrect;
                keptIds.Add(id);
            }
            else
            {
                Context.AnswerOptions.Add(new AnswerOption { QuestionId = entity.Id, Text = optionRequest.Text, IsCorrect = optionRequest.IsCorrect });
            }
        }

        Context.AnswerOptions.RemoveRange(existingOptions.Where(o => !keptIds.Contains(o.Id)));
    }

    protected override async Task BeforeDeleteAsync(Question entity, CancellationToken cancellationToken)
    {
        if (await Context.AttemptAnswers.AnyAsync(a => a.QuestionId == entity.Id, cancellationToken))
        {
            throw new BusinessException(Messages.QuestionHasAnswers);
        }
    }

    private static void ValidateQuestion(QuestionRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Text), request.Text, 1000, Messages.QuestionTextLabel);

        var options = request.AnswerOptions;
        if (options.Count != 4)
        {
            FieldValidationRules.Add(errors, nameof(QuestionRequest.AnswerOptions), Messages.QuestionNeedsFourOptions);
        }
        else
        {
            for (var i = 0; i < options.Count; i++)
            {
                FieldValidationRules.RequireText(errors, $"{nameof(QuestionRequest.AnswerOptions)}[{i}].{nameof(AnswerOptionRequest.Text)}", options[i].Text, 500, Messages.AnswerOptionTextLabel);
            }
            if (options.Count(o => o.IsCorrect) != 1)
            {
                FieldValidationRules.Add(errors, nameof(QuestionRequest.AnswerOptions), Messages.QuestionNeedsOneCorrectOption);
            }
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
