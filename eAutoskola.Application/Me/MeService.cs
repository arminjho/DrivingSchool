using eAutoskola.Application.Common;
using eAutoskola.Application.Users;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Files;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Security;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Me;

/// <summary>
/// userId is ALWAYS taken from ICurrentUserService (the JWT), never from a route or body
/// parameter (upute.md §5) - every method here operates on "the logged-in user", full stop.
/// </summary>
public class MeService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenBlocklistService _tokenBlocklistService;
    private readonly IFileStorageService _fileStorageService;

    public MeService(
        EAutoskolaDbContext context,
        ICurrentUserService currentUser,
        ITokenBlocklistService tokenBlocklistService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUser = currentUser;
        _tokenBlocklistService = tokenBlocklistService;
        _fileStorageService = fileStorageService;
    }

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserQuery().FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);
        return user.Adapt<UserDto>();
    }

    public async Task<UserDto> UpdateMeAsync(MeUpdateRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.FirstName), request.FirstName, 100, "Ime");
        FieldValidationRules.RequireText(errors, nameof(request.LastName), request.LastName, 100, "Prezime");
        FieldValidationRules.RequireEmail(errors, nameof(request.Email), request.Email);
        FieldValidationRules.RequirePhone(errors, nameof(request.PhoneNumber), request.PhoneNumber);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        if (errors.Count == 0 && user.Email != request.Email
            && await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Email), "Korisnik sa ovom e-mail adresom već postoji.");
        }

        FieldValidationRules.ThrowIfAny(errors);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = request.Address;
        user.CityId = request.CityId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetMeAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequirePassword(errors, nameof(request.NewPassword), request.NewPassword);
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            FieldValidationRules.Add(errors, nameof(request.ConfirmNewPassword), Messages.PasswordsDoNotMatch);
        }
        FieldValidationRules.ThrowIfAny(errors);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new ValidationException(nameof(request.OldPassword), Messages.OldPasswordIncorrect);
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);

        await _tokenBlocklistService.InvalidateTokensValidFromAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UploadProfileImageAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), _currentUser.UserId);

        var oldPath = user.ProfileImagePath;
        user.ProfileImagePath = await _fileStorageService.SaveImageAsync(content, fileName, contentType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(oldPath))
        {
            _fileStorageService.Delete(oldPath);
        }

        return await GetMeAsync(cancellationToken);
    }

    private IQueryable<User> GetCurrentUserQuery()
        => _context.Users
            .Include(u => u.City)
            .Include(u => u.StudentProfile)
            .Include(u => u.InstructorProfile)
            .Where(u => u.Id == _currentUser.UserId);
}
