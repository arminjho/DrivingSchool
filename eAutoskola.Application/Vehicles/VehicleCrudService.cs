using System.Text.RegularExpressions;
using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Vehicles;

/// <summary>
/// Named VehicleCrudService (not "VehicleService") to avoid colliding with the
/// eAutoskola.Domain.Entities.VehicleService entity (the maintenance-record entity).
/// </summary>
public partial class VehicleCrudService : BaseCRUDService<VehicleDto, VehicleSearchObject, VehicleInsertRequest, VehicleUpdateRequest, Vehicle>
{
    [GeneratedRegex(@"^[A-Z]\d{2}-[A-Z]-\d{3}$")]
    private static partial Regex RegistrationNumberPattern();

    public VehicleCrudService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<Vehicle> ApplyFilter(IQueryable<Vehicle> query, VehicleSearchObject search)
    {
        query = query.Include(v => v.VehicleCategory);

        if (!string.IsNullOrWhiteSpace(search.RegistrationNumber))
        {
            query = query.Where(v => v.RegistrationNumber.Contains(search.RegistrationNumber));
        }
        if (search.VehicleCategoryId.HasValue)
        {
            query = query.Where(v => v.VehicleCategoryId == search.VehicleCategoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search.Status) && Enum.TryParse<VehicleStatus>(search.Status, out var status))
        {
            query = query.Where(v => v.Status == status);
        }
        return query;
    }

    protected override IQueryable<Vehicle> GetByIdQuery(int id)
        => Context.Vehicles.Include(v => v.VehicleCategory).Where(v => v.Id == id);

    protected override async Task BeforeInsertAsync(Vehicle entity, VehicleInsertRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request.RegistrationNumber, null, cancellationToken);
        entity.Status = VehicleStatus.Active;
    }

    protected override async Task BeforeUpdateAsync(Vehicle entity, VehicleUpdateRequest request, CancellationToken cancellationToken)
        => await ValidateAsync(request.RegistrationNumber, entity.Id, cancellationToken);

    protected override async Task BeforeDeleteAsync(Vehicle entity, CancellationToken cancellationToken)
    {
        var hasFutureConfirmedLesson = await Context.Lessons.AnyAsync(
            l => l.VehicleId == entity.Id && l.Status == LessonStatus.Confirmed && l.StartsAtUtc > DateTime.UtcNow,
            cancellationToken);

        if (hasFutureConfirmedLesson)
        {
            throw new BusinessException(Messages.VehicleHasFutureLesson);
        }
    }

    private async Task ValidateAsync(string registrationNumber, int? excludingId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(registrationNumber) || !RegistrationNumberPattern().IsMatch(registrationNumber))
        {
            FieldValidationRules.Add(errors, nameof(VehicleInsertRequest.RegistrationNumber), Messages.RegistrationNumberInvalid);
        }
        else if (await Context.Vehicles.AnyAsync(v => v.RegistrationNumber == registrationNumber && v.Id != excludingId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(VehicleInsertRequest.RegistrationNumber), Messages.RegistrationNumberExists);
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
