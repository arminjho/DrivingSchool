using eAutoskola.Application.Users;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Tests.Payments;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace eAutoskola.Tests.Users;

/// <summary>Dorada #12: the desktop edit form showed the vehicle-category dropdown for a
/// Student/Instructor with its current value, but never sent it back on save, and UserService
/// never read it either, so changing it reported a successful save without changing anything.</summary>
public class UserServiceTests
{
    static UserServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(UserService).Assembly);

    private static PaymentTestFixture CreateFixture()
    {
        var f = new PaymentTestFixture();
        f.Context.VehicleCategories.AddRange(
            new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila", Description = "B" },
            new VehicleCategory { Id = 2, Code = "A", Name = "Motocikli", Description = "A" });
        f.Context.SaveChanges();
        return f;
    }

    private static UserUpdateRequest UpdateRequest(int? vehicleCategoryId) => new()
    {
        FirstName = "Ime", LastName = "Prezime", PhoneNumber = "+387 61 123 456", VehicleCategoryId = vehicleCategoryId,
    };

    [Fact]
    public async Task UpdatingAStudent_SavesTheNewVehicleCategoryOnTheProfile()
    {
        var f = CreateFixture();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);
        var student = await service.InsertAsync(new UserInsertRequest
        {
            Email = "ucenik@test.ba", Password = "Lozinka123", FirstName = "Ucenik", LastName = "Test",
            PhoneNumber = "+387 61 123 456", Role = Role.Student, VehicleCategoryId = 1,
        }, default);

        await service.UpdateAsync(student.Id, UpdateRequest(2), default);

        var profile = await f.Context.StudentProfiles.AsNoTracking().SingleAsync(p => p.UserId == student.Id);
        Assert.Equal(2, profile.VehicleCategoryId);
    }

    [Fact]
    public async Task UpdatingAnInstructor_SavesTheNewVehicleCategoryOnTheProfile()
    {
        var f = CreateFixture();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);
        var instructor = await service.InsertAsync(new UserInsertRequest
        {
            Email = "instruktor2@test.ba", Password = "Lozinka123", FirstName = "Instruktor", LastName = "Test",
            PhoneNumber = "+387 61 123 456", Role = Role.Instructor, VehicleCategoryId = 1,
        }, default);

        await service.UpdateAsync(instructor.Id, UpdateRequest(2), default);

        var profile = await f.Context.InstructorProfiles.AsNoTracking().SingleAsync(p => p.UserId == instructor.Id);
        Assert.Equal(2, profile.VehicleCategoryId);
    }

    [Fact]
    public async Task UpdatingAStudent_WithoutAVehicleCategory_IsRejected()
    {
        var f = CreateFixture();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);
        var student = await service.InsertAsync(new UserInsertRequest
        {
            Email = "ucenik2@test.ba", Password = "Lozinka123", FirstName = "Ucenik", LastName = "Test",
            PhoneNumber = "+387 61 123 456", Role = Role.Student, VehicleCategoryId = 1,
        }, default);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(student.Id, UpdateRequest(null), default));

        Assert.Contains(nameof(UserUpdateRequest.VehicleCategoryId), ex.Errors.Keys);
    }

    [Fact]
    public async Task UpdatingAnAdmin_NeedsNoVehicleCategory_AndTouchesNoProfile()
    {
        var f = CreateFixture();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);
        var admin = await service.InsertAsync(new UserInsertRequest
        {
            Email = "admin2@test.ba", Password = "Lozinka123", FirstName = "Admin", LastName = "Test",
            PhoneNumber = "+387 61 123 456", Role = Role.Admin,
        }, default);

        await service.UpdateAsync(admin.Id, UpdateRequest(null), default);

        Assert.Equal(0, await f.Context.StudentProfiles.CountAsync(p => p.UserId == admin.Id));
        Assert.Equal(0, await f.Context.InstructorProfiles.CountAsync(p => p.UserId == admin.Id));
    }
}
