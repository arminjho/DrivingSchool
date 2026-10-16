using eAutoskola.Application.Tests;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[Route("api/questions")]
[Authorize(Roles = Role.Admin)]
public class QuestionController : BaseCRUDController<QuestionDto, QuestionSearchObject, QuestionRequest, QuestionRequest>
{
    public QuestionController(QuestionCrudService service) : base(service)
    {
    }
}
