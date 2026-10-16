using eAutoskola.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController<TModel, TSearch> : ControllerBase
    where TSearch : BaseSearchObject
{
    protected readonly IQueryService<TModel, TSearch> Service;

    protected BaseController(IQueryService<TModel, TSearch> service)
    {
        Service = service;
    }

    [HttpGet]
    public virtual async Task<ActionResult<PagedResult<TModel>>> GetPaged([FromQuery] TSearch criteria, CancellationToken cancellationToken)
        => Ok(await Service.GetPagedAsync(criteria, cancellationToken));

    [HttpGet("{id:int}")]
    public virtual async Task<ActionResult<TModel>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await Service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
