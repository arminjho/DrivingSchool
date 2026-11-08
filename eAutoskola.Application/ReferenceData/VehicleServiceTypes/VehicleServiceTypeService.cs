using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.VehicleServiceTypes;

public class VehicleServiceTypeService : BaseCRUDService<VehicleServiceTypeDto, VehicleServiceTypeSearchObject, VehicleServiceTypeRequest, VehicleServiceTypeRequest, VehicleServiceType>
{
    public VehicleServiceTypeService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<VehicleServiceType> ApplyFilter(IQueryable<VehicleServiceType> query, VehicleServiceTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    protected override Task BeforeInsertAsync(VehicleServiceType entity, VehicleServiceTypeRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override Task BeforeUpdateAsync(VehicleServiceType entity, VehicleServiceTypeRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override async Task BeforeDeleteAsync(VehicleServiceType entity, CancellationToken cancellationToken)
    {
        var serviceCount = await Context.VehicleServices.CountAsync(s => s.VehicleServiceTypeId == entity.Id, cancellationToken);
        if (serviceCount > 0)
        {
            throw new BusinessException(string.Format(Messages.VehicleServiceTypeCannotBeDeleted, serviceCount));
        }
    }

    private static Task ValidateAsync(VehicleServiceTypeRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 150, Messages.NameLabel);
        FieldValidationRules.ThrowIfAny(errors);
        return Task.CompletedTask;
    }
}
