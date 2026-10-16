using eAutoskola.Application.Payments;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize(Roles = Role.Student)]
public class FinanceController : ControllerBase
{
    private readonly FinanceService _service;

    public FinanceController(FinanceService service)
    {
        _service = service;
    }

    [HttpGet("my")]
    public async Task<ActionResult<FinanceSummaryDto>> GetMy(CancellationToken cancellationToken)
        => Ok(await _service.GetMyAsync(cancellationToken));
}
