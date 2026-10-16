using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.Cities;

public class CityService : BaseCRUDService<CityDto, CitySearchObject, CityRequest, CityRequest, City>
{
    public CityService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<City> ApplyFilter(IQueryable<City> query, CitySearchObject search)
    {
        query = query.Include(c => c.Country);

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        if (search.CountryId.HasValue)
        {
            query = query.Where(c => c.CountryId == search.CountryId.Value);
        }
        return query;
    }

    protected override IQueryable<City> GetByIdQuery(int id)
        => Context.Cities.Include(c => c.Country).Where(c => c.Id == id);
}
