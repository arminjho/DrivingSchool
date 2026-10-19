using eAutoskola.Application.Common;
using eAutoskola.Application.StudyLessons;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/study-lessons")]
public class StudyLessonController : BaseCRUDController<StudyLessonDto, StudyLessonSearchObject, StudyLessonRequest, StudyLessonRequest>
{
    private readonly StudyLessonCrudService _service;

    public StudyLessonController(StudyLessonCrudService service) : base(service)
    {
        _service = service;
    }

    public override async Task<ActionResult<StudyLessonDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetDetailByIdAsync(id, cancellationToken));

    [HttpGet("my")]
    [Authorize(Roles = Role.Student)]
    public async Task<ActionResult<PagedResult<StudentStudyLessonDto>>> GetMy([FromQuery] PageQuery criteria, CancellationToken cancellationToken)
        => Ok(await _service.GetMyLessonsAsync(criteria, cancellationToken));

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<StudyLessonDto>> Insert(StudyLessonRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<StudyLessonDto>> Update(int id, StudyLessonRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
