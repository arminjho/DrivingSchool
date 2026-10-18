using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>Reads require a token like every other reference-data controller; the registration
/// form (which has no token yet) gets its own narrow lookup via `GET /api/auth/register-lookups`
/// instead, keeping [AllowAnonymous] confined to the auth endpoints (upute.md §5).</summary>
[Route("api/cities")]
public class CityController : BaseCRUDController<CityDto, CitySearchObject, CityRequest, CityRequest>
{
    public CityController(ICRUDService<CityDto, CitySearchObject, CityRequest, CityRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CityDto>> Insert(CityRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CityDto>> Update(int id, CityRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
