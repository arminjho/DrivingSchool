using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.RequestStatuses;

public static class RequestStatusServiceCollectionExtensions
{
    public static IServiceCollection AddRequestStatusServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<RequestStatusDto, RequestStatusSearchObject, RequestStatusRequest, RequestStatusRequest>, RequestStatusService>();
}
