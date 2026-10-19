using eAutoskola.Application.VehicleMaintenance;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Fleet;

/// <summary>A service that starts puts its vehicle "Na servisu"; completing it returns the vehicle
/// to "Aktivan" - both stored by the single save of the update.</summary>
public class VehicleMaintenanceServiceTests
{
    static VehicleMaintenanceServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(VehicleMaintenanceService).Assembly);

    private static (VehicleMaintenanceService Service, EAutoskolaDbContext Context, VehicleService Record) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" });
        context.VehicleServiceTypes.Add(new VehicleServiceType { Id = 1, Name = "Redovni servis" });
        context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021 });
        var record = new VehicleService { Id = 1, VehicleId = 1, VehicleServiceTypeId = 1, ServiceDate = DateTime.UtcNow, Description = "Servis", Cost = 50m };
        context.VehicleServices.Add(record);
        context.SaveChanges();
        context.ChangeTracker.Clear();
        return (new VehicleMaintenanceService(context), context, record);
    }

    private static VehicleServiceUpdateRequest Update(VehicleServiceStatus status) => new()
    {
        ServiceDate = DateTime.UtcNow, VehicleServiceTypeId = 1, Description = "Servis", Cost = 50m, Status = status,
    };

    [Fact]
    public async Task InProgress_PutsTheVehicleInService_AndCompletedReturnsItToActive()
    {
        var (service, context, record) = Create();

        await service.UpdateAsync(record.Id, Update(VehicleServiceStatus.InProgress), default);
        Assert.Equal(VehicleStatus.InService, (await context.Vehicles.AsNoTracking().SingleAsync()).Status);

        context.ChangeTracker.Clear();
        await service.UpdateAsync(record.Id, Update(VehicleServiceStatus.Completed), default);
        Assert.Equal(VehicleStatus.Active, (await context.Vehicles.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task UpdateWithoutAStatusChange_LeavesTheVehicleAlone()
    {
        var (service, context, record) = Create();

        await service.UpdateAsync(record.Id, Update(VehicleServiceStatus.Scheduled), default);

        Assert.Equal(VehicleStatus.Active, (await context.Vehicles.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Insert_ForAVehicleThatDoesNotExist_IsAValidationError()
    {
        var (service, _, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(
            new VehicleServiceInsertRequest { VehicleId = 99, VehicleServiceTypeId = 1, ServiceDate = DateTime.UtcNow, Description = "x", Cost = 1m }, default));
    }
}
