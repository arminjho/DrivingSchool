using eAutoskola.Application.VehicleMaintenance;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/vehicle-services")]
[Authorize(Roles = Role.Admin)]
public class VehicleServiceController : BaseCRUDController<VehicleServiceDto, VehicleServiceSearchObject, VehicleServiceInsertRequest, VehicleServiceUpdateRequest>
{
    public VehicleServiceController(VehicleMaintenanceService service) : base(service)
    {
    }
}
