using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.Cities;

public static class CityServiceCollectionExtensions
{
    public static IServiceCollection AddCityServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<CityDto, CitySearchObject, CityRequest, CityRequest>, CityService>();
}
