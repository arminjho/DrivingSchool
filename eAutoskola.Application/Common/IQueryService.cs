namespace eAutoskola.Application.Common;

public interface IQueryService<TModel, in TSearch> where TSearch : BaseSearchObject
{
    Task<PagedResult<TModel>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default);
    Task<TModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
