using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.ServiceTypes;

public class ServiceTypeService : BaseCRUDService<ServiceTypeDto, ServiceTypeSearchObject, ServiceTypeRequest, ServiceTypeRequest, ServiceType>
{
    public ServiceTypeService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<ServiceType> ApplyFilter(IQueryable<ServiceType> query, ServiceTypeSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    // Code is not part of the request: the payment logic looks the well-known rows up by Code
    // (seeded), so admins can rename/reprice them freely but can't break that link. Rows an
    // admin adds get a generated code.
    protected override Task BeforeInsertAsync(ServiceType entity, ServiceTypeRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        entity.Code = $"CUSTOM_{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        return Task.CompletedTask;
    }

    protected override Task BeforeUpdateAsync(ServiceType entity, ServiceTypeRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return Task.CompletedTask;
    }

    protected override async Task BeforeDeleteAsync(ServiceType entity, CancellationToken cancellationToken)
    {
        var chargeCount = await Context.Charges.CountAsync(c => c.ServiceTypeId == entity.Id, cancellationToken);
        if (chargeCount > 0)
        {
            throw new BusinessException(string.Format(Messages.ServiceTypeCannotBeDeleted, chargeCount));
        }
    }

    private static void Validate(ServiceTypeRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 150, Messages.NameLabel);
        if (request.Price < 0)
        {
            FieldValidationRules.Add(errors, nameof(request.Price), Messages.NegativePriceNotAllowed);
        }
        FieldValidationRules.ThrowIfAny(errors);
    }
}
