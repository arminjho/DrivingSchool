using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.VehicleServiceTypes;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/vehicle-service-types")]
public class VehicleServiceTypeController : BaseCRUDController<VehicleServiceTypeDto, VehicleServiceTypeSearchObject, VehicleServiceTypeRequest, VehicleServiceTypeRequest>
{
    public VehicleServiceTypeController(ICRUDService<VehicleServiceTypeDto, VehicleServiceTypeSearchObject, VehicleServiceTypeRequest, VehicleServiceTypeRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<VehicleServiceTypeDto>> Insert(VehicleServiceTypeRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<VehicleServiceTypeDto>> Update(int id, VehicleServiceTypeRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
