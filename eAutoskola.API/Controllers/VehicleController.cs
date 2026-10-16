using eAutoskola.Application.Vehicles;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/vehicles")]
[Authorize(Roles = Role.Admin)]
public class VehicleController : BaseCRUDController<VehicleDto, VehicleSearchObject, VehicleInsertRequest, VehicleUpdateRequest>
{
    public VehicleController(VehicleCrudService service) : base(service)
    {
    }
}
