using eAutoskola.Application.ReferenceData.VehicleServiceTypes;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#11: VehicleServiceTypeService had no validation and no delete-reason
/// message.</summary>
public class VehicleServiceTypeServiceTests
{
    static VehicleServiceTypeServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(VehicleServiceTypeService).Assembly);

    private static (VehicleServiceTypeService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.VehicleServiceTypes.Add(new VehicleServiceType { Id = 1, Name = "Redovni servis" });
        context.SaveChanges();
        return (new VehicleServiceTypeService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankName_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new VehicleServiceTypeRequest { Name = "" }));
    }

    [Fact]
    public async Task Delete_ATypeUsedByAVehicleService_IsRejectedWithTheServiceCount()
    {
        var (service, context) = Create();
        context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" });
        context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021 });
        context.VehicleServices.Add(new VehicleService { Id = 1, VehicleId = 1, VehicleServiceTypeId = 1, ServiceDate = DateTime.UtcNow, Description = "Servis", Cost = 50m });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_AnUnusedType_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.VehicleServiceTypes.CountAsync());
    }
}
