using eAutoskola.Application.Tests;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/test-attempts")]
[Authorize(Roles = Role.Student)]
public class TestAttemptController : ControllerBase
{
    private readonly TestAttemptService _attemptService;

    public TestAttemptController(TestAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<TestResultDto>> Submit(int id, SubmitTestAttemptRequest request, CancellationToken cancellationToken)
        => Ok(await _attemptService.SubmitAttemptAsync(id, request, cancellationToken));

    [HttpGet("{id:int}/review")]
    public async Task<ActionResult<TestAttemptReviewDto>> Review(int id, CancellationToken cancellationToken)
        => Ok(await _attemptService.GetReviewAsync(id, cancellationToken));
}
