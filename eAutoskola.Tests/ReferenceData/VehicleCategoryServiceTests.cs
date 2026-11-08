using eAutoskola.Application.ReferenceData.VehicleCategories;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#11: VehicleCategoryService had no validation and no delete-reason
/// message, even though three different tables (Vehicles, StudentProfiles,
/// InstructorProfiles) can reference a category.</summary>
public class VehicleCategoryServiceTests
{
    static VehicleCategoryServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(VehicleCategoryService).Assembly);

    private static (VehicleCategoryService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" });
        context.SaveChanges();
        return (new VehicleCategoryService(context), context);
    }

    [Fact]
    public async Task Insert_WithACodeLongerThanOneCharacter_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new VehicleCategoryRequest { Code = "BB", Name = "Nova kategorija" }));
    }

    [Fact]
    public async Task Insert_WithACodeThatAlreadyExists_IsRejectedWithASpecificMessage()
    {
        var (service, _) = Create();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new VehicleCategoryRequest { Code = "B", Name = "Duplikat" }));
        Assert.Contains(ex.Errors[nameof(VehicleCategoryRequest.Code)], m => m.Contains('B'));
    }

    [Fact]
    public async Task Insert_WithADescriptionOver300Characters_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(
            new VehicleCategoryRequest { Code = "A", Name = "Motocikli", Description = new string('x', 301) }));
    }

    [Fact]
    public async Task Delete_ACategoryUsedByAVehicle_IsRejectedWithTheVehicleCount()
    {
        var (service, context) = Create();
        context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021 });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_ACategoryUsedByAStudent_IsRejectedWithTheStudentCount()
    {
        var (service, context) = Create();
        context.StudentProfiles.Add(new StudentProfile { UserId = 1, VehicleCategoryId = 1 });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
    }

    [Fact]
    public async Task Delete_AnUnusedCategory_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.VehicleCategories.CountAsync());
    }
}
