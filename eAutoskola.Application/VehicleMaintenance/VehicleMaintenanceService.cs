using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.VehicleMaintenance;

/// <summary>
/// Named VehicleMaintenanceService to avoid a confusing "VehicleServiceService" double-Service
/// name, since the entity itself is already called VehicleService.
/// </summary>
public class VehicleMaintenanceService : BaseCRUDService<VehicleServiceDto, VehicleServiceSearchObject, VehicleServiceInsertRequest, VehicleServiceUpdateRequest, VehicleService>
{
    public VehicleMaintenanceService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<VehicleService> ApplyFilter(IQueryable<VehicleService> query, VehicleServiceSearchObject search)
    {
        query = query.Include(s => s.Vehicle).Include(s => s.VehicleServiceType);

        if (search.VehicleId.HasValue)
        {
            query = query.Where(s => s.VehicleId == search.VehicleId.Value);
        }
        if (search.VehicleCategoryId.HasValue)
        {
            query = query.Where(s => s.Vehicle!.VehicleCategoryId == search.VehicleCategoryId.Value);
        }
        if (search.FromDate.HasValue)
        {
            query = query.Where(s => s.ServiceDate >= search.FromDate.Value);
        }
        if (search.ToDate.HasValue)
        {
            query = query.Where(s => s.ServiceDate <= search.ToDate.Value);
        }
        return query;
    }

    protected override IQueryable<VehicleService> GetByIdQuery(int id)
        => Context.VehicleServices.Include(s => s.Vehicle).Include(s => s.VehicleServiceType).Where(s => s.Id == id);

    protected override async Task BeforeInsertAsync(VehicleService entity, VehicleServiceInsertRequest request, CancellationToken cancellationToken)
    {
        if (request.Cost < 0)
        {
            throw new ValidationException(nameof(request.Cost), Messages.NegativeCostNotAllowed);
        }
        if (!await Context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new ValidationException(nameof(request.VehicleId), Messages.VehicleNotFound);
        }
        entity.Status = VehicleServiceStatus.Scheduled;
    }

    /// <summary>A vehicle's Status is derived from ALL of its services, not just the one being
    /// saved: InService while at least one service (other than the one just saved/deleted) is
    /// still InProgress, Active only once none is. The request has already been mapped onto the
    /// entity when this runs, so the status the service had before is read from EF's tracked
    /// original value.</summary>
    protected override async Task BeforeUpdateAsync(VehicleService entity, VehicleServiceUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.Cost < 0)
        {
            throw new ValidationException(nameof(request.Cost), Messages.NegativeCostNotAllowed);
        }

        var previousStatus = Context.Entry(entity).Property(s => s.Status).OriginalValue;
        if (previousStatus == request.Status)
        {
            return;
        }

        await RecomputeVehicleStatusAsync(entity.VehicleId, entity.Id, request.Status == VehicleServiceStatus.InProgress, cancellationToken);
    }

    /// <summary>Deleting an InProgress service must not leave the vehicle stuck InService
    /// forever with no open service left - recompute from what remains, in the same save as the
    /// deletion.</summary>
    protected override async Task BeforeDeleteAsync(VehicleService entity, CancellationToken cancellationToken)
    {
        if (entity.Status != VehicleServiceStatus.InProgress)
        {
            return;
        }

        await RecomputeVehicleStatusAsync(entity.VehicleId, entity.Id, consideredInProgress: false, cancellationToken);
    }

    /// <summary>Both the service's own save and the vehicle's status update commit in the same
    /// SaveChangesAsync that follows the Before*Async hook (upute.md §3.4) - the DB query below
    /// still sees the saved-service's PREVIOUS row (nothing has been persisted yet), so its own
    /// new/removed status is passed in as <paramref name="consideredInProgress"/> instead.</summary>
    private async Task RecomputeVehicleStatusAsync(int vehicleId, int excludingServiceId, bool consideredInProgress, CancellationToken cancellationToken)
    {
        var hasOtherInProgress = await Context.VehicleServices.AsNoTracking()
            .AnyAsync(s => s.VehicleId == vehicleId && s.Id != excludingServiceId && s.Status == VehicleServiceStatus.InProgress, cancellationToken);

        var vehicle = await Context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), vehicleId);

        vehicle.Status = hasOtherInProgress || consideredInProgress ? VehicleStatus.InService : VehicleStatus.Active;
    }
}
