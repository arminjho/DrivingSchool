using eAutoskola.Application.Common;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
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

    protected override Task BeforeInsertAsync(DomainRequestStatus entity, RequestStatusRequest request, CancellationToken cancellationToken)
        => ValidateAsync(null, request, cancellationToken);

    /// <summary>The Code of the four lesson statuses is what ties the row to the LessonStatus enum,
    /// so it cannot change; their name and description can.</summary>
    protected override async Task BeforeUpdateAsync(DomainRequestStatus entity, RequestStatusRequest request, CancellationToken cancellationToken)
    {
        var originalCode = Context.Entry(entity).Property(s => s.Code).OriginalValue;
        if (IsLessonStatus(originalCode) && originalCode != entity.Code)
        {
            throw new BusinessException(Messages.RequestStatusCodeFixed);
        }

        await ValidateAsync(entity, request, cancellationToken);
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

    private async Task ValidateAsync(DomainRequestStatus? entity, RequestStatusRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Code), request.Code, 30, Messages.CodeLabel);
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 100, Messages.NameLabel);
        if (!string.IsNullOrEmpty(request.Description) && request.Description.Length > 300)
        {
            FieldValidationRules.Add(errors, nameof(request.Description), string.Format(Messages.MaxLengthExceeded, Messages.DescriptionLabel, 300));
        }

        var excludingId = entity?.Id ?? 0;
        if (errors.Count == 0 && await Context.RequestStatuses.AnyAsync(s => s.Code == request.Code && s.Id != excludingId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Code), string.Format(Messages.RequestStatusCodeAlreadyExists, request.Code));
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
