using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.QuestionAreas;

public class QuestionAreaService : BaseCRUDService<QuestionAreaDto, QuestionAreaSearchObject, QuestionAreaRequest, QuestionAreaRequest, QuestionArea>
{
    public QuestionAreaService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<QuestionArea> ApplyFilter(IQueryable<QuestionArea> query, QuestionAreaSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    protected override Task BeforeInsertAsync(QuestionArea entity, QuestionAreaRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override Task BeforeUpdateAsync(QuestionArea entity, QuestionAreaRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    /// <summary>Checked in this order because any one of them already explains why - a question
    /// area used by questions is virtually always also used by its test and study lessons.</summary>
    protected override async Task BeforeDeleteAsync(QuestionArea entity, CancellationToken cancellationToken)
    {
        var questionCount = await Context.Questions.CountAsync(q => q.QuestionAreaId == entity.Id, cancellationToken);
        if (questionCount > 0)
        {
            throw new BusinessException(string.Format(Messages.QuestionAreaUsedByQuestions, questionCount));
        }

        var testCount = await Context.Tests.CountAsync(t => t.QuestionAreaId == entity.Id, cancellationToken);
        if (testCount > 0)
        {
            throw new BusinessException(string.Format(Messages.QuestionAreaUsedByTests, testCount));
        }

        var studyLessonCount = await Context.StudyLessons.CountAsync(l => l.QuestionAreaId == entity.Id, cancellationToken);
        if (studyLessonCount > 0)
        {
            throw new BusinessException(string.Format(Messages.QuestionAreaUsedByStudyLessons, studyLessonCount));
        }
    }

    private static Task ValidateAsync(QuestionAreaRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 150, Messages.NameLabel);
        FieldValidationRules.ThrowIfAny(errors);
        return Task.CompletedTask;
    }
}
