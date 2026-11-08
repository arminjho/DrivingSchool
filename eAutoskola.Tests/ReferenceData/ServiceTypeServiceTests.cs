using eAutoskola.Application.ReferenceData.ServiceTypes;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#10/#11: ServiceTypeService had no validation (name, negative price) and
/// no delete-reason message.</summary>
public class ServiceTypeServiceTests
{
    static ServiceTypeServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(ServiceTypeService).Assembly);

    private static (ServiceTypeService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.ServiceTypes.Add(new ServiceType { Id = 1, Code = "THEORY", Name = "Teorijska obuka", Price = 100m });
        context.SaveChanges();
        return (new ServiceTypeService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankName_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new ServiceTypeRequest { Name = " ", Price = 10m }));
    }

    /// <summary>Dorada #10: ServiceTypeRequest.Price had no check - a negative price reached the
    /// PDF reports.</summary>
    [Fact]
    public async Task Insert_WithANegativePrice_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new ServiceTypeRequest { Name = "Dodatni čas", Price = -5m }));
    }

    [Fact]
    public async Task Insert_WithValidData_GeneratesItsOwnCode()
    {
        var (service, context) = Create();

        var dto = await service.InsertAsync(new ServiceTypeRequest { Name = "Dodatni čas", Price = 20m });

        var stored = await context.ServiceTypes.AsNoTracking().SingleAsync(s => s.Id == dto.Id);
        Assert.StartsWith("CUSTOM_", stored.Code);
    }

    [Fact]
    public async Task Delete_AServiceTypeWithCharges_IsRejectedWithTheChargeCount()
    {
        var (service, context) = Create();
        context.Users.Add(new User { Id = 1, Email = "s@test.ba", PasswordHash = "x", Role = Role.Student, FirstName = "A", LastName = "B", PhoneNumber = "+387 61 000 001" });
        context.Charges.Add(new Charge { Id = 1, StudentId = 1, ServiceTypeId = 1, Amount = 100m, Description = "Teorijska obuka" });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_AServiceTypeWithNoCharges_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.ServiceTypes.CountAsync());
    }
}
