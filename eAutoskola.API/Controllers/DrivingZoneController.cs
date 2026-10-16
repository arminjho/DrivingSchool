using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.DrivingZones;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/driving-zones")]
public class DrivingZoneController : BaseCRUDController<DrivingZoneDto, DrivingZoneSearchObject, DrivingZoneRequest, DrivingZoneRequest>
{
    public DrivingZoneController(ICRUDService<DrivingZoneDto, DrivingZoneSearchObject, DrivingZoneRequest, DrivingZoneRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<DrivingZoneDto>> Insert(DrivingZoneRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<DrivingZoneDto>> Update(int id, DrivingZoneRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
