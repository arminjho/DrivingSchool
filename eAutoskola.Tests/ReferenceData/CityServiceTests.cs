using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#11: CityService had no validation and no delete-reason message.</summary>
public class CityServiceTests
{
    static CityServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(CityService).Assembly);

    private static (CityService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Countries.Add(new Country { Id = 1, Code = "BA", Name = "Bosna i Hercegovina" });
        context.Cities.Add(new City { Id = 1, Name = "Sarajevo", PostalCode = "71000", CountryId = 1 });
        context.SaveChanges();
        return (new CityService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankPostalCode_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new CityRequest { Name = "Mostar", PostalCode = " ", CountryId = 1 }));
    }

    [Fact]
    public async Task Insert_ForACountryThatDoesNotExist_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new CityRequest { Name = "Mostar", PostalCode = "88000", CountryId = 99 }));
    }

    [Fact]
    public async Task Insert_WithValidData_Succeeds()
    {
        var (service, context) = Create();

        await service.InsertAsync(new CityRequest { Name = "Mostar", PostalCode = "88000", CountryId = 1 });

        Assert.Equal(2, await context.Cities.CountAsync());
    }

    /// <summary>The professor's own example message: "Grad nije moguće obrisati jer ga koristi
    /// 12 korisnika." - a count-bearing, specific reason instead of the generic FK message.</summary>
    [Fact]
    public async Task Delete_ACityWithUsers_IsRejectedWithTheUserCount()
    {
        var (service, context) = Create();
        context.Users.Add(new User { Id = 1, Email = "u@test.ba", PasswordHash = "x", Role = Role.Student, FirstName = "A", LastName = "B", PhoneNumber = "+387 61 000 001", CityId = 1 });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_ACityWithNoUsers_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.Cities.CountAsync());
    }
}
