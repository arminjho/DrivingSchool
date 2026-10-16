using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.VehicleCategories;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/vehicle-categories")]
public class VehicleCategoryController : BaseCRUDController<VehicleCategoryDto, VehicleCategorySearchObject, VehicleCategoryRequest, VehicleCategoryRequest>
{
    public VehicleCategoryController(ICRUDService<VehicleCategoryDto, VehicleCategorySearchObject, VehicleCategoryRequest, VehicleCategoryRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<VehicleCategoryDto>> Insert(VehicleCategoryRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<VehicleCategoryDto>> Update(int id, VehicleCategoryRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
