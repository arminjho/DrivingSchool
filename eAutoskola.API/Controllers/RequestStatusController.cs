using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.RequestStatuses;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/request-statuses")]
public class RequestStatusController : BaseCRUDController<RequestStatusDto, RequestStatusSearchObject, RequestStatusRequest, RequestStatusRequest>
{
    public RequestStatusController(ICRUDService<RequestStatusDto, RequestStatusSearchObject, RequestStatusRequest, RequestStatusRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<RequestStatusDto>> Insert(RequestStatusRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<RequestStatusDto>> Update(int id, RequestStatusRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
