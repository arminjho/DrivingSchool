using eAutoskola.Application.Common;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
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

    /// <summary>The Code of the four lesson statuses is what ties the row to the LessonStatus enum,
    /// so it cannot change; their name and description can.</summary>
    protected override Task BeforeUpdateAsync(DomainRequestStatus entity, RequestStatusRequest request, CancellationToken cancellationToken)
    {
        var originalCode = Context.Entry(entity).Property(s => s.Code).OriginalValue;
        if (IsLessonStatus(originalCode) && originalCode != entity.Code)
        {
            throw new BusinessException(Messages.RequestStatusCodeFixed);
        }
        return Task.CompletedTask;
    }

    /// <summary>The state machine looks these rows up when a lesson is booked or changes status.</summary>
    protected override Task BeforeDeleteAsync(DomainRequestStatus entity, CancellationToken cancellationToken)
    {
        if (IsLessonStatus(entity.Code))
        {
            throw new BusinessException(Messages.RequestStatusUsedByLessonFlow);
        }
        return Task.CompletedTask;
    }

    private static bool IsLessonStatus(string code) => Enum.GetNames<LessonStatus>().Contains(code);
}
