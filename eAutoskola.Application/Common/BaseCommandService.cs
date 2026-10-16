using eAutoskola.Infrastructure.Database;

namespace eAutoskola.Application.Common;

public abstract class BaseCommandService<TModel, TInsert, TUpdate, TDbEntity> : ICommandService<TModel, TInsert, TUpdate>
    where TDbEntity : class
{
    protected readonly EAutoskolaDbContext Context;

    protected BaseCommandService(EAutoskolaDbContext context)
    {
        Context = context;
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

    /// <summary>Exposed for derived services that need to persist extra changes (e.g. a
    /// related entity) within the same SaveChanges call as an Insert/Update/Delete.</summary>
    protected Task SaveChangesAsync(CancellationToken cancellationToken) => CrudOperations<TModel, TInsert, TUpdate, TDbEntity>.SaveChangesAsync(Context, cancellationToken);
}
