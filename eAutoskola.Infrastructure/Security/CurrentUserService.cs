using System.Security.Claims;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Http;

namespace eAutoskola.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var value = FindClaim(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : throw new AuthenticationException(Messages.NotAuthenticated);
        }
    }

    public string Role => FindClaim(ClaimTypes.Role)
        ?? throw new AuthenticationException(Messages.NotAuthenticated);

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;

    private string? FindClaim(string claimType)
        => _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
}
