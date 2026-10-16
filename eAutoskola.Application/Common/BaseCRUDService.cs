using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.Common;

/// <summary>
/// Combined query+command base for simple reference/codebook entities (City, VehicleCategory,
/// etc.) - identical shape to the reference project's own BaseCRUDService. Entities with real
/// command complexity use BaseQueryService + BaseCommandService as two separate services
/// instead (see docs/REFERENCE_ANALYSIS.md §3).
/// </summary>
public abstract class BaseCRUDService<TModel, TSearch, TInsert, TUpdate, TDbEntity>
    : BaseQueryService<TModel, TSearch, TDbEntity>, ICRUDService<TModel, TSearch, TInsert, TUpdate>
    where TSearch : BaseSearchObject
    where TDbEntity : class
{
    protected BaseCRUDService(EAutoskolaDbContext context) : base(context)
    {
    }

    public virtual Task<TModel> InsertAsync(TInsert request, CancellationToken cancellationToken = default)
        => CrudOperations<TModel, TInsert, TUpdate, TDbEntity>.InsertAsync(Context, request, BeforeInsertAsync, AfterInsertAsync, cancellationToken);

    public virtual Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default)
        => CrudOperations<TModel, TInsert, TUpdate, TDbEntity>.UpdateAsync(Context, id, request, BeforeUpdateAsync, AfterUpdateAsync, cancellationToken);

    public virtual Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        => CrudOperations<TModel, TInsert, TUpdate, TDbEntity>.DeleteAsync(Context, id, BeforeDeleteAsync, cancellationToken);

    protected virtual Task BeforeInsertAsync(TDbEntity entity, TInsert request, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task AfterInsertAsync(TDbEntity entity, TInsert request, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task BeforeUpdateAsync(TDbEntity entity, TUpdate request, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task AfterUpdateAsync(TDbEntity entity, TUpdate request, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task BeforeDeleteAsync(TDbEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    protected Task SaveChangesAsync(CancellationToken cancellationToken) => CrudOperations<TModel, TInsert, TUpdate, TDbEntity>.SaveChangesAsync(Context, cancellationToken);
}
