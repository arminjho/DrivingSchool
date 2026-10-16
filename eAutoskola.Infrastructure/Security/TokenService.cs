using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace eAutoskola.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(JwtOptions options)
    {
        _options = options;
    }

    public (string Token, string Jti, DateTime ExpiresAtUtc) IssueToken(User user)
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, expiresAtUtc);
    }
}
