using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;

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
        entity.Code = $"CUSTOM_{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        return Task.CompletedTask;
    }
}
