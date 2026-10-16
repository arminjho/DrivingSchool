using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.ReferenceData.VehicleServiceTypes;

public class VehicleServiceTypeService : BaseCRUDService<VehicleServiceTypeDto, VehicleServiceTypeSearchObject, VehicleServiceTypeRequest, VehicleServiceTypeRequest, VehicleServiceType>
{
    public VehicleServiceTypeService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<VehicleServiceType> ApplyFilter(IQueryable<VehicleServiceType> query, VehicleServiceTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }
}
