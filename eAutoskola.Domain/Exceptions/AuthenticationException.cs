namespace eAutoskola.Domain.Exceptions;

/// <summary>
/// Bad credentials, disabled account, or an invalid/expired/revoked token. Distinct from
/// <see cref="ForbiddenException"/> so the client can special-case "go back to login".
/// </summary>
public class AuthenticationException : EAutoskolaException
{
    public AuthenticationException(string message) : base(message)
    {
    }
}
