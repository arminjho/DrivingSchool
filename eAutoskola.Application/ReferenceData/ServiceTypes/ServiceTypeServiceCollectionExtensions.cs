using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.ServiceTypes;

public static class ServiceTypeServiceCollectionExtensions
{
    public static IServiceCollection AddServiceTypeServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<ServiceTypeDto, ServiceTypeSearchObject, ServiceTypeRequest, ServiceTypeRequest>, ServiceTypeService>();
}
