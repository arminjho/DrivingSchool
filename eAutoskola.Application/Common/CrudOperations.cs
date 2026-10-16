using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Common;

/// <summary>
/// Shared Insert/Update/Delete implementation used by both BaseCommandService (split
/// Query/Command entities) and BaseCRUDService (simple codebook entities) - avoids C#'s lack
/// of multiple inheritance forcing an awkward delegation wrapper between the two.
/// </summary>
internal static class CrudOperations<TModel, TInsert, TUpdate, TDbEntity>
    where TDbEntity : class
{
    public static async Task<TModel> InsertAsync(
        EAutoskolaDbContext context, TInsert request,
        Func<TDbEntity, TInsert, CancellationToken, Task> beforeInsert,
        Func<TDbEntity, TInsert, CancellationToken, Task> afterInsert,
        CancellationToken cancellationToken)
    {
        var entity = request.Adapt<TDbEntity>();

        await beforeInsert(entity, request, cancellationToken);
        context.Set<TDbEntity>().Add(entity);
        await SaveChangesAsync(context, cancellationToken);
        await afterInsert(entity, request, cancellationToken);

        return entity.Adapt<TModel>();
    }

    public static async Task<TModel> UpdateAsync(
        EAutoskolaDbContext context, int id, TUpdate request,
        Func<TDbEntity, TUpdate, CancellationToken, Task> beforeUpdate,
        Func<TDbEntity, TUpdate, CancellationToken, Task> afterUpdate,
        CancellationToken cancellationToken)
    {
        var entity = await context.Set<TDbEntity>().FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(typeof(TDbEntity).Name, id);

        request.Adapt(entity);

        await beforeUpdate(entity, request, cancellationToken);
        await SaveChangesAsync(context, cancellationToken);
        await afterUpdate(entity, request, cancellationToken);

        return entity.Adapt<TModel>();
    }

    public static async Task DeleteAsync(
        EAutoskolaDbContext context, int id,
        Func<TDbEntity, CancellationToken, Task> beforeDelete,
        CancellationToken cancellationToken)
    {
        var entity = await context.Set<TDbEntity>().FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(typeof(TDbEntity).Name, id);

        await beforeDelete(entity, cancellationToken);

        if (entity is ISoftDelete softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = DateTime.UtcNow;
        }
        else
        {
            context.Set<TDbEntity>().Remove(entity);
        }

        await SaveChangesAsync(context, cancellationToken);
    }

    /// <summary>Translates an FK-violation DbUpdateException into a Bosnian BusinessException
    /// instead of letting a raw SQL error reach the client.</summary>
    public static async Task SaveChangesAsync(EAutoskolaDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new BusinessException(Messages.RecordInUseCannotModify, ex);
        }
    }
}
