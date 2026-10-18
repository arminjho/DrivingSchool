using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/cities")]
public class CityController : BaseCRUDController<CityDto, CitySearchObject, CityRequest, CityRequest>
{
    public CityController(ICRUDService<CityDto, CitySearchObject, CityRequest, CityRequest> service) : base(service)
    {
    }

    // Anonymous reads only: the Student self-registration form needs a city dropdown before it
    // has a token to call anything else with (upute.md §5 still protects writes, below).
    [AllowAnonymous]
    public override Task<ActionResult<PagedResult<CityDto>>> GetPaged(CitySearchObject criteria, CancellationToken cancellationToken) => base.GetPaged(criteria, cancellationToken);

    [AllowAnonymous]
    public override Task<ActionResult<CityDto>> GetById(int id, CancellationToken cancellationToken) => base.GetById(id, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CityDto>> Insert(CityRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CityDto>> Update(int id, CityRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
