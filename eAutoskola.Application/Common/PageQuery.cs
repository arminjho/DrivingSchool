namespace eAutoskola.Application.Common;

/// <summary>Plain page/pageSize query for the "my ..." list endpoints that have no filters of
/// their own. BaseSearchObject is abstract and cannot bind from the query string directly; its
/// PageSize clamp (max 100) applies here too.</summary>
public class PageQuery : BaseSearchObject;
