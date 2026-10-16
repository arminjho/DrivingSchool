using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.VehicleServiceTypes;

public static class VehicleServiceTypeServiceCollectionExtensions
{
    public static IServiceCollection AddVehicleServiceTypeServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<VehicleServiceTypeDto, VehicleServiceTypeSearchObject, VehicleServiceTypeRequest, VehicleServiceTypeRequest>, VehicleServiceTypeService>();
}
