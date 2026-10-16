using eAutoskola.Application.Common;
using eAutoskola.Infrastructure.Database;
using DomainRequestStatus = eAutoskola.Domain.Entities.RequestStatus;

namespace eAutoskola.Application.ReferenceData.RequestStatuses;

public class RequestStatusService : BaseCRUDService<RequestStatusDto, RequestStatusSearchObject, RequestStatusRequest, RequestStatusRequest, DomainRequestStatus>
{
    public RequestStatusService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<DomainRequestStatus> ApplyFilter(IQueryable<DomainRequestStatus> query, RequestStatusSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }
}
