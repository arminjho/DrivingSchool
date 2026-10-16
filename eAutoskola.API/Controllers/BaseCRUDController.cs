using eAutoskola.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

public abstract class BaseCRUDController<TModel, TSearch, TInsert, TUpdate> : BaseController<TModel, TSearch>
    where TSearch : BaseSearchObject
{
    protected new readonly ICRUDService<TModel, TSearch, TInsert, TUpdate> Service;

    protected BaseCRUDController(ICRUDService<TModel, TSearch, TInsert, TUpdate> service) : base(service)
    {
        Service = service;
    }

    [HttpPost]
    public virtual async Task<ActionResult<TModel>> Insert(TInsert request, CancellationToken cancellationToken)
    {
        var created = await Service.InsertAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<ActionResult<TModel>> Update(int id, TUpdate request, CancellationToken cancellationToken)
        => Ok(await Service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await Service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
