using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace eAutoskola.Application.Common;

/// <summary>
/// Holds the concrete EAutoskolaDbContext directly (no repository/IApplicationDbContext
/// abstraction) - matches the accepted reference project's pattern exactly, per the approved
/// architecture decision in docs/REFERENCE_ANALYSIS.md §3. Registered Scoped (upute.md §3.4).
/// </summary>
public abstract class BaseQueryService<TModel, TSearch, TDbEntity> : IQueryService<TModel, TSearch>
    where TSearch : BaseSearchObject
    where TDbEntity : class
{
    protected readonly EAutoskolaDbContext Context;

    protected BaseQueryService(EAutoskolaDbContext context)
    {
        Context = context;
    }

    public virtual async Task<PagedResult<TModel>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<TDbEntity>().AsNoTracking();
        query = ApplyFilter(query, search);

        var count = await query.CountAsync(cancellationToken);

        query = ApplySorting(query, search);
        query = query.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize);

        var entities = await query.ToListAsync(cancellationToken);

        return new PagedResult<TModel>
        {
            ResultList = entities.Adapt<List<TModel>>(),
            Count = count,
        };
    }

    public virtual async Task<TModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdQuery(id).FirstOrDefaultAsync(cancellationToken);
        return entity is null ? default : entity.Adapt<TModel>();
    }

    /// <summary>Override to add .Where() filters from the search object.</summary>
    protected virtual IQueryable<TDbEntity> ApplyFilter(IQueryable<TDbEntity> query, TSearch search) => query;

    /// <summary>Override to add .Include()s for GetById (list queries stay projection-only, no
    /// Include, per upute.md §8.2's N+1 guidance).</summary>
    protected virtual IQueryable<TDbEntity> GetByIdQuery(int id) =>
        Context.Set<TDbEntity>().Where(EfCoreIdPredicate(id));

    private static System.Linq.Expressions.Expression<Func<TDbEntity, bool>> EfCoreIdPredicate(int id)
        => e => EF.Property<int>(e, "Id") == id;

    private IQueryable<TDbEntity> ApplySorting(IQueryable<TDbEntity> query, TSearch search)
    {
        if (!search.IsSortable(search.SortBy))
        {
            return query;
        }

        var direction = string.Equals(search.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "descending" : "ascending";
        return query.OrderBy($"{search.SortBy} {direction}");
    }
}
