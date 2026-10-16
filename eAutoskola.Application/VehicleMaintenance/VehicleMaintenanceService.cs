using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
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
        if (!await Context.Vehicles.AnyAsync(v => v.Id == request.VehicleId, cancellationToken))
        {
            throw new ValidationException(nameof(request.VehicleId), "Odabrano vozilo ne postoji.");
        }
        entity.Status = VehicleServiceStatus.Scheduled;
    }

    /// <summary>Setting a service to InProgress puts the vehicle InService; completing it
    /// returns the vehicle to Active - both saved in the single SaveChangesAsync that follows
    /// this hook (one transaction, upute.md §3.4).</summary>
    protected override async Task BeforeUpdateAsync(VehicleService entity, VehicleServiceUpdateRequest request, CancellationToken cancellationToken)
    {
        if (entity.Status == request.Status)
        {
            return;
        }

        var vehicle = await Context.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), entity.VehicleId);

        if (request.Status == VehicleServiceStatus.InProgress)
        {
            vehicle.Status = VehicleStatus.InService;
        }
        else if (request.Status == VehicleServiceStatus.Completed)
        {
            vehicle.Status = VehicleStatus.Active;
        }
    }
}
