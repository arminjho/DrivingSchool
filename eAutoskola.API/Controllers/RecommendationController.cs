using eAutoskola.Application.Recommendations;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize(Roles = Role.Student)]
public class RecommendationController : ControllerBase
{
    private readonly StudyRecommendationService _service;

    public RecommendationController(StudyRecommendationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<RecommendationsResultDto>> Get([FromQuery] int take, CancellationToken cancellationToken)
        => Ok(await _service.GetRecommendationsAsync(take <= 0 ? RecommenderConstants.DefaultRecommendationCount : take, cancellationToken));
}
