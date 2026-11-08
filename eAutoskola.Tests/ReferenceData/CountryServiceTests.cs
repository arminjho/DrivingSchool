using eAutoskola.Application.ReferenceData.Countries;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9: CountryService had no server-side validation at all - an empty name, a
/// too-long name, or a duplicate Code all reached the database and surfaced as the same generic
/// "record in use" message.</summary>
public class CountryServiceTests
{
    static CountryServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(CountryService).Assembly);

    private static (CountryService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Countries.Add(new Country { Id = 1, Code = "BA", Name = "Bosna i Hercegovina" });
        context.SaveChanges();
        return (new CountryService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankName_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new CountryRequest { Code = "HR", Name = "  " }));
    }

    [Fact]
    public async Task Insert_WithACodeLongerThanTwoCharacters_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new CountryRequest { Code = "HRV", Name = "Hrvatska" }));
    }

    /// <summary>The professor's exact example: a duplicate code must say so, not surface as the
    /// generic FK-violation message.</summary>
    [Fact]
    public async Task Insert_WithACodeThatAlreadyExists_IsRejectedWithASpecificMessage()
    {
        var (service, _) = Create();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new CountryRequest { Code = "BA", Name = "Neka druga država" }));
        Assert.Contains(ex.Errors[nameof(CountryRequest.Code)], m => m.Contains("BA"));
    }

    [Fact]
    public async Task Update_KeepingItsOwnCode_IsNotTreatedAsADuplicate()
    {
        var (service, context) = Create();

        await service.UpdateAsync(1, new CountryRequest { Code = "BA", Name = "Bosna i Hercegovina (izmijenjeno)" });

        Assert.Equal("Bosna i Hercegovina (izmijenjeno)", (await context.Countries.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task Delete_ACountryWithCities_IsRejectedWithTheCityCount()
    {
        var (service, context) = Create();
        context.Cities.Add(new City { Id = 1, Name = "Sarajevo", PostalCode = "71000", CountryId = 1 });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_ACountryWithNoCities_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.Countries.CountAsync());
    }
}
