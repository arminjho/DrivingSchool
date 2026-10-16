namespace eAutoskola.Domain.Security;

/// <summary>
/// The ONLY way a service learns who the current user is - userId is never accepted from a
/// route or request body for "current user" operations (upute.md §5). Implemented in
/// Infrastructure using IHttpContextAccessor, injected into Application services instead of
/// services parsing the token themselves. Lives in Domain (not Application) because
/// Infrastructure - which implements it - has no project reference to Application (see
/// docs/REFERENCE_ANALYSIS.md §3's dependency direction).
/// </summary>
public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    bool IsInRole(string role);
}
