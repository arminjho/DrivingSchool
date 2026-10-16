using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
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

    protected override async Task BeforeDeleteAsync(Test entity, CancellationToken cancellationToken)
    {
        if (await Context.TestAttempts.AnyAsync(a => a.TestId == entity.Id, cancellationToken))
        {
            throw new BusinessException("Test se ne može obrisati jer postoje evidentirani pokušaji rješavanja.");
        }
    }
}
