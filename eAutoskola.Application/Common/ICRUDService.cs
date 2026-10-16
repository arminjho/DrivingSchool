namespace eAutoskola.Application.Common;

/// <summary>
/// Combines Query + Command for simple reference/codebook entities where splitting into
/// separate Query/Command services would be pure ceremony - see docs/REFERENCE_ANALYSIS.md
/// §3. Entities with real command complexity (Lesson, etc.) implement IQueryService and
/// ICommandService as two separate interfaces/classes instead.
/// </summary>
public interface ICRUDService<TModel, in TSearch, in TInsert, in TUpdate>
    : IQueryService<TModel, TSearch>, ICommandService<TModel, TInsert, TUpdate>
    where TSearch : BaseSearchObject;
