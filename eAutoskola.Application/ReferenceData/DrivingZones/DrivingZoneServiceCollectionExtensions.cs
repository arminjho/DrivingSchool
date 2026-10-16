using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.DrivingZones;

public static class DrivingZoneServiceCollectionExtensions
{
    public static IServiceCollection AddDrivingZoneServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<DrivingZoneDto, DrivingZoneSearchObject, DrivingZoneRequest, DrivingZoneRequest>, DrivingZoneService>();
}
