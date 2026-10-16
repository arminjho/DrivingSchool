using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

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
}
