using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.DrivingZones;

public class DrivingZoneService : BaseCRUDService<DrivingZoneDto, DrivingZoneSearchObject, DrivingZoneRequest, DrivingZoneRequest, DrivingZone>
{
    public DrivingZoneService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<DrivingZone> ApplyFilter(IQueryable<DrivingZone> query, DrivingZoneSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    protected override Task BeforeInsertAsync(DrivingZone entity, DrivingZoneRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override Task BeforeUpdateAsync(DrivingZone entity, DrivingZoneRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request);

    protected override async Task BeforeDeleteAsync(DrivingZone entity, CancellationToken cancellationToken)
    {
        var lessonCount = await Context.Lessons.CountAsync(l => l.DrivingZoneId == entity.Id, cancellationToken);
        if (lessonCount > 0)
        {
            throw new BusinessException(string.Format(Messages.DrivingZoneCannotBeDeleted, lessonCount));
        }
    }

    private static Task ValidateAsync(DrivingZoneRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 100, Messages.NameLabel);
        FieldValidationRules.ThrowIfAny(errors);
        return Task.CompletedTask;
    }
}
