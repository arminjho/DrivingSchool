using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Tests;

public class TestCrudService : BaseCRUDService<TestDto, TestSearchObject, TestRequest, TestRequest, Test>
{
    public TestCrudService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<Test> ApplyFilter(IQueryable<Test> query, TestSearchObject search)
    {
        query = query.Include(t => t.QuestionArea).Include(t => t.Questions);

        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(t => t.Title.Contains(search.Title));
        }
        if (search.QuestionAreaId.HasValue)
        {
            query = query.Where(t => t.QuestionAreaId == search.QuestionAreaId.Value);
        }
        return query;
    }

    protected override IQueryable<Test> GetByIdQuery(int id)
        => Context.Tests.Include(t => t.QuestionArea).Include(t => t.Questions).Where(t => t.Id == id);

    protected override Task BeforeInsertAsync(Test entity, TestRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override Task BeforeUpdateAsync(Test entity, TestRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    private static Task ValidateAsync(TestRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.PassThresholdPercent is < 0 or > 100)
        {
            FieldValidationRules.Add(errors, nameof(request.PassThresholdPercent), Messages.PassThresholdOutOfRange);
        }
        FieldValidationRules.ThrowIfAny(errors);
        return Task.CompletedTask;
    }

    protected override async Task BeforeDeleteAsync(Test entity, CancellationToken cancellationToken)
    {
        if (await Context.TestAttempts.AnyAsync(a => a.TestId == entity.Id, cancellationToken))
        {
            throw new BusinessException(Messages.TestHasAttempts);
        }
    }
}
