using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.VehicleCategories;

public static class VehicleCategoryServiceCollectionExtensions
{
    public static IServiceCollection AddVehicleCategoryServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<VehicleCategoryDto, VehicleCategorySearchObject, VehicleCategoryRequest, VehicleCategoryRequest>, VehicleCategoryService>();
}
