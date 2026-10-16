using eAutoskola.Application.Lessons;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/lessons")]
[Authorize]
public class LessonController : ControllerBase
{
    private readonly LessonQueryService _queryService;
    private readonly LessonCommandService _commandService;

    public LessonController(LessonQueryService queryService, LessonCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;
    }

    [HttpGet("available-slots")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<List<DateTime>>> GetAvailableSlots([FromQuery] int instructorId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
        => Ok(await _queryService.GetAvailableSlotsAsync(instructorId, date, cancellationToken));

    [HttpGet("my")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<eAutoskola.Application.Common.PagedResult<LessonDto>>> GetMy([FromQuery] MyLessonsSearchObject criteria, CancellationToken cancellationToken)
        => Ok(await _queryService.GetMyLessonsAsync(criteria, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LessonDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _queryService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<LessonDto>> Book(LessonBookRequest request, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await _commandService.BookAsync(request, cancellationToken));

    [HttpPut("{id:int}/reschedule")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<LessonDto>> Reschedule(int id, LessonRescheduleRequest request, CancellationToken cancellationToken)
        => Ok(await _commandService.RescheduleAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = $"{Role.Instructor},{Role.Admin}")]
    public async Task<ActionResult<LessonDto>> Confirm(int id, CancellationToken cancellationToken)
        => Ok(await _commandService.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = $"{Role.Instructor},{Role.Admin}")]
    public async Task<ActionResult<LessonDto>> Reject(int id, LessonReasonRequest request, CancellationToken cancellationToken)
        => Ok(await _commandService.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = $"{Role.Instructor},{Role.Admin}")]
    public async Task<ActionResult<LessonDto>> Start(int id, CancellationToken cancellationToken)
        => Ok(await _commandService.StartAsync(id, cancellationToken));

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = $"{Role.Instructor},{Role.Admin}")]
    public async Task<ActionResult<LessonDto>> Complete(int id, LessonCompleteRequest request, CancellationToken cancellationToken)
        => Ok(await _commandService.CompleteAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<LessonDto>> Cancel(int id, [FromBody] LessonReasonRequest? request, CancellationToken cancellationToken)
        => Ok(await _commandService.CancelAsync(id, request, cancellationToken));
}
