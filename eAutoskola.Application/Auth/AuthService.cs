using System.Security.Cryptography;
using eAutoskola.Application.Auth.Dto;
using eAutoskola.Application.Auth.Requests;
using eAutoskola.Application.Common;
using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Application.ReferenceData.VehicleCategories;
using eAutoskola.Domain.Resources;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Messaging;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Payments;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Auth;

/// <summary>
/// Holds the concrete EAutoskolaDbContext directly, same pattern as every other Application
/// service (docs/REFERENCE_ANALYSIS.md §3). Registered Scoped.
/// </summary>
public class AuthService : IAuthService
{
    private static readonly TimeSpan ResetCodeValidity = TimeSpan.FromMinutes(15);

    private readonly EAutoskolaDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly ITokenBlocklistService _tokenBlocklistService;
    private readonly IMessagePublisher _messagePublisher;

    public AuthService(
        EAutoskolaDbContext context,
        ITokenService tokenService,
        ITokenBlocklistService tokenBlocklistService,
        IMessagePublisher messagePublisher)
    {
        _context = context;
        _tokenService = tokenService;
        _tokenBlocklistService = tokenBlocklistService;
        _messagePublisher = messagePublisher;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireEmail(errors, nameof(request.Email), request.Email);
        FieldValidationRules.RequirePassword(errors, nameof(request.Password), request.Password);
        FieldValidationRules.RequireText(errors, nameof(request.FirstName), request.FirstName, 100, "Ime");
        FieldValidationRules.RequireText(errors, nameof(request.LastName), request.LastName, 100, "Prezime");
        FieldValidationRules.RequirePhone(errors, nameof(request.PhoneNumber), request.PhoneNumber);

        if (request.Password != request.ConfirmPassword)
        {
            FieldValidationRules.Add(errors, nameof(request.ConfirmPassword), Messages.PasswordsDoNotMatch);
        }

        if (errors.Count == 0 && await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Email), "Korisnik sa ovom e-mail adresom već postoji.");
        }

        if (errors.Count == 0 && !await _context.VehicleCategories.AnyAsync(c => c.Id == request.VehicleCategoryId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.VehicleCategoryId), "Odabrana kategorija vozila ne postoji.");
        }

        FieldValidationRules.ThrowIfAny(errors);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = Role.Student,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CityId = request.CityId,
            StudentProfile = new StudentProfile
            {
                VehicleCategoryId = request.VehicleCategoryId,
                RegisteredAt = DateTime.UtcNow,
            },
        };

        // User, profile and package charges are saved together - a student never exists
        // without them.
        _context.Users.Add(user);
        await PackageChargeProvisioner.AddPackageChargesAsync(_context, user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return IssueAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException(Messages.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            throw new AuthenticationException(Messages.AccountDisabled);
        }

        return IssueAuthResponse(user);
    }

    public Task LogoutAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
        => _tokenBlocklistService.RevokeAsync(jti, expiresAtUtc, cancellationToken);

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
        {
            // Never reveal whether an email is registered - still "succeeds" from the caller's
            // perspective.
            return;
        }

        var code = GenerateResetCode();

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            CodeHash = PasswordHasher.Hash(code),
            ExpiresAt = DateTime.UtcNow.Add(ResetCodeValidity),
        });
        await _context.SaveChangesAsync(cancellationToken);

        await _messagePublisher.PublishAsync(new PasswordResetRequestedMessage
        {
            ToEmail = user.Email,
            ToName = $"{user.FirstName} {user.LastName}",
            Code = code,
        }, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new ValidationException(nameof(request.ConfirmNewPassword), Messages.PasswordsDoNotMatch);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null)
        {
            throw new ValidationException(nameof(request.Email), Messages.InvalidOrExpiredResetCode);
        }

        var resetToken = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetToken is null || !PasswordHasher.Verify(request.Code, resetToken.CodeHash))
        {
            throw new ValidationException(nameof(request.Code), Messages.InvalidOrExpiredResetCode);
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        resetToken.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);

        await _tokenBlocklistService.InvalidateTokensValidFromAsync(user.Id, cancellationToken);
    }

    private AuthResponseDto IssueAuthResponse(User user)
    {
        var (token, _, expiresAtUtc) = _tokenService.IssueToken(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
        };
    }

    /// <summary>Cryptographically random 6-digit code - never System.Random
    /// (upute.md Appendix A.3).</summary>
    private static string GenerateResetCode() => RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

    /// <summary>The two reference-data lists the registration form needs before it has a token.
    /// A small bounded reference set (same as every dropdown elsewhere in the app), so returning
    /// it in full needs no paging.</summary>
    public async Task<RegisterLookupsDto> GetRegisterLookupsAsync(CancellationToken cancellationToken = default)
    {
        var cities = await _context.Cities
            .Include(c => c.Country)
            .OrderBy(c => c.Name)
            .Select(c => new CityDto { Id = c.Id, Name = c.Name, PostalCode = c.PostalCode, CountryId = c.CountryId, CountryName = c.Country!.Name })
            .ToListAsync(cancellationToken);

        var vehicleCategories = await _context.VehicleCategories
            .OrderBy(c => c.Code)
            .Select(c => new VehicleCategoryDto { Id = c.Id, Code = c.Code, Name = c.Name, Description = c.Description })
            .ToListAsync(cancellationToken);

        return new RegisterLookupsDto { Cities = cities, VehicleCategories = vehicleCategories };
    }
}
