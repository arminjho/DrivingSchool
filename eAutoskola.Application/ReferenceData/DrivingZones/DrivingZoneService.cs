using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.ReferenceData.DrivingZones;

public class DrivingZoneService : BaseCRUDService<DrivingZoneDto, DrivingZoneSearchObject, DrivingZoneRequest, DrivingZoneRequest, DrivingZone>
{
    public DrivingZoneService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<DrivingZone> ApplyFilter(IQueryable<DrivingZone> query, DrivingZoneSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }
}
