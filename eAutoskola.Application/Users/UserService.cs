using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using eAutoskola.Infrastructure.Payments;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Users;

public class UserService : BaseCRUDService<UserDto, UserSearchObject, UserInsertRequest, UserUpdateRequest, User>
{
    private readonly ITokenBlocklistService _tokenBlocklistService;

    public UserService(EAutoskolaDbContext context, ITokenBlocklistService tokenBlocklistService) : base(context)
    {
        _tokenBlocklistService = tokenBlocklistService;
    }

    protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserSearchObject search)
    {
        query = query.Include(u => u.City).Include(u => u.StudentProfile).Include(u => u.InstructorProfile);

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            query = query.Where(u =>
                u.FirstName.Contains(search.Search) ||
                u.LastName.Contains(search.Search) ||
                u.Email.Contains(search.Search));
        }
        if (!string.IsNullOrWhiteSpace(search.Role))
        {
            query = query.Where(u => u.Role == search.Role);
        }
        return query;
    }

    protected override IQueryable<User> GetByIdQuery(int id)
        => Context.Users.Include(u => u.City).Include(u => u.StudentProfile).Include(u => u.InstructorProfile).Where(u => u.Id == id);

    protected override async Task BeforeInsertAsync(User entity, UserInsertRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireEmail(errors, nameof(request.Email), request.Email);
        FieldValidationRules.RequirePassword(errors, nameof(request.Password), request.Password);
        FieldValidationRules.RequireText(errors, nameof(request.FirstName), request.FirstName, 100, "Ime");
        FieldValidationRules.RequireText(errors, nameof(request.LastName), request.LastName, 100, "Prezime");
        FieldValidationRules.RequirePhone(errors, nameof(request.PhoneNumber), request.PhoneNumber);

        if (!Role.All.Contains(request.Role))
        {
            FieldValidationRules.Add(errors, nameof(request.Role), "Odabrana uloga nije validna.");
        }
        if (request.Role is Role.Student or Role.Instructor && request.VehicleCategoryId is null)
        {
            FieldValidationRules.Add(errors, nameof(request.VehicleCategoryId), Messages.RequiredField);
        }
        if (errors.Count == 0 && await Context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Email), "Korisnik sa ovom e-mail adresom već postoji.");
        }

        FieldValidationRules.ThrowIfAny(errors);

        entity.PasswordHash = PasswordHasher.Hash(request.Password);

        // The profile (and, for a student, the package charges) are attached to the user here so
        // the base class's single SaveChangesAsync stores everything in one transaction.
        if (request.Role == Role.Student)
        {
            entity.StudentProfile = new StudentProfile { VehicleCategoryId = request.VehicleCategoryId!.Value };
            await PackageChargeProvisioner.AddPackageChargesAsync(Context, entity, cancellationToken);
        }
        else if (request.Role == Role.Instructor)
        {
            entity.InstructorProfile = new InstructorProfile { VehicleCategoryId = request.VehicleCategoryId!.Value };
        }
    }

    protected override async Task BeforeUpdateAsync(User entity, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.FirstName), request.FirstName, 100, "Ime");
        FieldValidationRules.RequireText(errors, nameof(request.LastName), request.LastName, 100, "Prezime");
        FieldValidationRules.RequirePhone(errors, nameof(request.PhoneNumber), request.PhoneNumber);

        if (!string.IsNullOrEmpty(request.NewPassword) || !string.IsNullOrEmpty(request.ConfirmNewPassword))
        {
            FieldValidationRules.RequirePassword(errors, nameof(request.NewPassword), request.NewPassword);
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                FieldValidationRules.Add(errors, nameof(request.ConfirmNewPassword), Messages.PasswordsDoNotMatch);
            }
        }

        FieldValidationRules.ThrowIfAny(errors);

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            entity.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            await _tokenBlocklistService.InvalidateTokensValidFromAsync(entity.Id, cancellationToken);
        }
    }

    protected override async Task BeforeDeleteAsync(User entity, CancellationToken cancellationToken)
    {
        var hasLessonHistory = await Context.Lessons.AnyAsync(l => l.StudentId == entity.Id || l.InstructorId == entity.Id, cancellationToken);
        if (hasLessonHistory)
        {
            throw new BusinessException("Korisnik se ne može obrisati jer ima evidentirane časove u sistemu.");
        }
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        user.IsActive = isActive;
        if (!isActive)
        {
            await _tokenBlocklistService.InvalidateTokensValidFromAsync(id, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
