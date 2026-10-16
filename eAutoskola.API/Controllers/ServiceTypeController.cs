using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.ServiceTypes;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/service-types")]
public class ServiceTypeController : BaseCRUDController<ServiceTypeDto, ServiceTypeSearchObject, ServiceTypeRequest, ServiceTypeRequest>
{
    public ServiceTypeController(ICRUDService<ServiceTypeDto, ServiceTypeSearchObject, ServiceTypeRequest, ServiceTypeRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<ServiceTypeDto>> Insert(ServiceTypeRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<ServiceTypeDto>> Update(int id, ServiceTypeRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
