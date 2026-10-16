namespace eAutoskola.Application.Common;

public interface ICommandService<TModel, in TInsert, in TUpdate>
{
    Task<TModel> InsertAsync(TInsert request, CancellationToken cancellationToken = default);
    Task<TModel> UpdateAsync(int id, TUpdate request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
