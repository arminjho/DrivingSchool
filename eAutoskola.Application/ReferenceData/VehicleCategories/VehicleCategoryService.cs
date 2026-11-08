using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.VehicleCategories;

public class VehicleCategoryService : BaseCRUDService<VehicleCategoryDto, VehicleCategorySearchObject, VehicleCategoryRequest, VehicleCategoryRequest, VehicleCategory>
{
    public VehicleCategoryService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<VehicleCategory> ApplyFilter(IQueryable<VehicleCategory> query, VehicleCategorySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    protected override Task BeforeInsertAsync(VehicleCategory entity, VehicleCategoryRequest request, CancellationToken cancellationToken)
        => ValidateAsync(null, request, cancellationToken);

    protected override Task BeforeUpdateAsync(VehicleCategory entity, VehicleCategoryRequest request, CancellationToken cancellationToken)
        => ValidateAsync(entity, request, cancellationToken);

    /// <summary>Checked in this order because any one of them already explains why - a category
    /// in use by a vehicle is usually also the category of at least one student or instructor.</summary>
    protected override async Task BeforeDeleteAsync(VehicleCategory entity, CancellationToken cancellationToken)
    {
        var vehicleCount = await Context.Vehicles.CountAsync(v => v.VehicleCategoryId == entity.Id, cancellationToken);
        if (vehicleCount > 0)
        {
            throw new BusinessException(string.Format(Messages.VehicleCategoryUsedByVehicles, vehicleCount));
        }

        var studentCount = await Context.StudentProfiles.CountAsync(p => p.VehicleCategoryId == entity.Id, cancellationToken);
        if (studentCount > 0)
        {
            throw new BusinessException(string.Format(Messages.VehicleCategoryUsedByStudents, studentCount));
        }

        var instructorCount = await Context.InstructorProfiles.CountAsync(p => p.VehicleCategoryId == entity.Id, cancellationToken);
        if (instructorCount > 0)
        {
            throw new BusinessException(string.Format(Messages.VehicleCategoryUsedByInstructors, instructorCount));
        }
    }

    private async Task ValidateAsync(VehicleCategory? entity, VehicleCategoryRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Code), request.Code, 1, Messages.CodeLabel);
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 100, Messages.NameLabel);
        if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 300)
        {
            FieldValidationRules.Add(errors, nameof(request.Description), string.Format(Messages.MaxLengthExceeded, Messages.DescriptionLabel, 300));
        }

        var excludingId = entity?.Id ?? 0;
        if (errors.Count == 0 && await Context.VehicleCategories.AnyAsync(c => c.Code == request.Code && c.Id != excludingId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Code), string.Format(Messages.VehicleCategoryCodeAlreadyExists, request.Code));
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
