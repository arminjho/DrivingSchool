using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.QuestionAreas;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/question-areas")]
public class QuestionAreaController : BaseCRUDController<QuestionAreaDto, QuestionAreaSearchObject, QuestionAreaRequest, QuestionAreaRequest>
{
    public QuestionAreaController(ICRUDService<QuestionAreaDto, QuestionAreaSearchObject, QuestionAreaRequest, QuestionAreaRequest> service) : base(service)
    {
    }

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<QuestionAreaDto>> Insert(QuestionAreaRequest request, CancellationToken cancellationToken) => base.Insert(request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<ActionResult<QuestionAreaDto>> Update(int id, QuestionAreaRequest request, CancellationToken cancellationToken) => base.Update(id, request, cancellationToken);

    [Authorize(Roles = Role.Admin)]
    public override Task<IActionResult> Delete(int id, CancellationToken cancellationToken) => base.Delete(id, cancellationToken);
}
