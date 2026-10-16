using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.ReferenceData.VehicleCategories;

public class VehicleCategoryService : BaseCRUDService<VehicleCategoryDto, VehicleCategorySearchObject, VehicleCategoryRequest, VehicleCategoryRequest, VehicleCategory>
{
    public VehicleCategoryService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<VehicleCategory> ApplyFilter(IQueryable<VehicleCategory> query, VehicleCategorySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }
}
