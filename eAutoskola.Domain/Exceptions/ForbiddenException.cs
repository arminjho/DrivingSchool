namespace eAutoskola.Domain.Exceptions;

/// <summary>
/// The caller is authenticated but not allowed to perform this action on this resource
/// (e.g. a Student trying to modify another student's lesson). Distinct from
/// <see cref="AuthenticationException"/>, which means "not logged in / bad credentials".
/// </summary>
public class ForbiddenException : EAutoskolaException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
