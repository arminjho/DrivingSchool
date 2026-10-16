using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.ReferenceData.Countries;

public class CountryService : BaseCRUDService<CountryDto, CountrySearchObject, CountryRequest, CountryRequest, Country>
{
    public CountryService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<Country> ApplyFilter(IQueryable<Country> query, CountrySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }
}
