using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.Countries;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>Authenticated read (dropdowns), Admin-only writes (upute.md §3.1).</summary>
[Route("api/countries")]
public class CountryController : BaseCRUDController<CountryDto, CountrySearchObject, CountryRequest, CountryRequest>
{
    public CountryController(ICRUDService<CountryDto, CountrySearchObject, CountryRequest, CountryRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CountryDto>> Insert(CountryRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<CountryDto>> Update(int id, CountryRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
