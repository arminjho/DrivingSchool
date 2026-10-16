using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.Countries;

public static class CountryServiceCollectionExtensions
{
    public static IServiceCollection AddCountryServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<CountryDto, CountrySearchObject, CountryRequest, CountryRequest>, CountryService>();
}
