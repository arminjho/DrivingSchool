using eAutoskola.Application.StudyLessons;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/study-lesson-sections")]
[Authorize(Roles = Role.Admin)]
public class StudyLessonSectionController : BaseCRUDController<StudyLessonSectionDto, StudyLessonSectionSearchObject, StudyLessonSectionRequest, StudyLessonSectionRequest>
{
    public StudyLessonSectionController(StudyLessonSectionCrudService service) : base(service)
    {
    }
}
