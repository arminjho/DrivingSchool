using eAutoskola.Application.Tests;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

public class TestController : BaseCRUDController<TestDto, TestSearchObject, TestRequest, TestRequest>
{
    private readonly TestAttemptService _attemptService;

    public TestController(TestCrudService service, TestAttemptService attemptService) : base(service)
    {
        _attemptService = attemptService;
    }

    [HttpGet("my")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<List<TestSummaryDto>>> GetMy(CancellationToken cancellationToken)
        => Ok(await _attemptService.GetMyTestsAsync(cancellationToken));

    [HttpPost("{id:int}/attempts")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<TestAttemptStartDto>> StartAttempt(int id, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _attemptService.StartAttemptAsync(id, cancellationToken));

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<TestDto>> Insert(TestRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<TestDto>> Update(int id, TestRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
