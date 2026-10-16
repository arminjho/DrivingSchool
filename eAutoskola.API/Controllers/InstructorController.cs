using eAutoskola.Application.Common;
using eAutoskola.Application.Instructors;
using eAutoskola.Application.Lessons;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>
/// Route stays plural "api/instructors" throughout (including the lesson-facing actions
/// below) for internal consistency - the two source prompts used "api/instructors/..." for
/// the public lookup/students endpoints and "api/instructor/..." for the lesson-facing ones,
/// which would otherwise split one logical resource across two inconsistently-named routes.
/// </summary>
[Route("api/instructors")]
public class InstructorController : BaseController<InstructorDto, InstructorSearchObject>
{
    private readonly InstructorQueryService _instructorQueryService;
    private readonly LessonQueryService _lessonQueryService;

    public InstructorController(InstructorQueryService instructorQueryService, LessonQueryService lessonQueryService) : base(instructorQueryService)
    {
        _instructorQueryService = instructorQueryService;
        _lessonQueryService = lessonQueryService;
    }

    [HttpGet("me/students")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<PagedResult<StudentSummaryDto>>> GetMyStudents([FromQuery] StudentSearchObject criteria, CancellationToken cancellationToken)
        => Ok(await _instructorQueryService.GetMyStudentsAsync(criteria, cancellationToken));

    [HttpGet("lesson-requests")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<PagedResult<LessonDto>>> GetLessonRequests([FromQuery] LessonRequestSearchObject criteria, CancellationToken cancellationToken)
        => Ok(await _lessonQueryService.GetLessonRequestsAsync(criteria, cancellationToken));

    [HttpGet("lesson-requests/counts")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<LessonRequestCountsDto>> GetLessonRequestCounts(CancellationToken cancellationToken)
        => Ok(await _lessonQueryService.GetLessonRequestCountsAsync(cancellationToken));

    [HttpGet("schedule")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<List<LessonDto>>> GetSchedule([FromQuery] DateOnly date, CancellationToken cancellationToken)
        => Ok(await _lessonQueryService.GetScheduleAsync(date, cancellationToken));

    [HttpGet("statistics")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<InstructorStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
        => Ok(await _lessonQueryService.GetStatisticsAsync(cancellationToken));

    [HttpGet("home")]
    [Authorize(Roles = Role.Instructor)]
    public async Task<ActionResult<InstructorHomeDto>> GetHome(CancellationToken cancellationToken)
        => Ok(await _lessonQueryService.GetHomeAsync(cancellationToken));
}
