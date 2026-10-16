using eAutoskola.Application.Auth;
using eAutoskola.Application.Auth.Requests;
using eAutoskola.Application.Users;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace eAutoskola.Tests.Payments;

/// <summary>A student never exists without a profile and the three package charges - they are
/// saved in the same SaveChanges as the user, whichever way the account is created.</summary>
public class StudentCreationTests
{
    static StudentCreationTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(UserService).Assembly);

    private static PaymentTestFixture CreateFixture()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        f.Context.VehicleCategories.Add(new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila", Description = "B" });
        f.Context.SaveChanges();
        return f;
    }

    private static UserInsertRequest NewUserRequest(string role) => new()
    {
        Email = $"{role.ToLowerInvariant()}.novi@test.ba", Password = "Lozinka123", FirstName = "Novi", LastName = "Korisnik",
        PhoneNumber = "+387 61 123 456", Role = role, VehicleCategoryId = role == Role.Admin ? null : 1,
    };

    [Fact]
    public async Task AdminCreatesStudent_UserProfileAndPackageChargesAreSavedTogether()
    {
        var f = CreateFixture();
        var saves = f.CountSaves();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);

        var dto = await service.InsertAsync(NewUserRequest(Role.Student), default);

        Assert.Equal(1, saves());
        Assert.NotNull(await f.Context.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == dto.Id));
        var charges = await f.Context.Charges.Include(c => c.Installments).Where(c => c.StudentId == dto.Id).ToListAsync();
        Assert.Equal(3, charges.Count);
        Assert.Equal(1550m, charges.Sum(c => c.Installments.Sum(i => i.Amount)));
    }

    [Fact]
    public async Task AdminCreatesInstructor_GetsAProfileButNoCharges()
    {
        var f = CreateFixture();
        var saves = f.CountSaves();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);

        var dto = await service.InsertAsync(NewUserRequest(Role.Instructor), default);

        Assert.Equal(1, saves());
        Assert.NotNull(await f.Context.InstructorProfiles.SingleOrDefaultAsync(p => p.UserId == dto.Id));
        Assert.Equal(0, await f.Context.Charges.CountAsync());
    }

    [Fact]
    public async Task AdminCreatesAdmin_GetsNeitherProfileNorCharges()
    {
        var f = CreateFixture();
        var service = new UserService(f.Context, new Mock<ITokenBlocklistService>().Object);

        var dto = await service.InsertAsync(NewUserRequest(Role.Admin), default);

        Assert.Equal(0, await f.Context.StudentProfiles.CountAsync(p => p.UserId == dto.Id));
        Assert.Equal(0, await f.Context.Charges.CountAsync());
    }

    [Fact]
    public async Task SelfRegistration_UserProfileAndPackageChargesAreSavedTogether()
    {
        var f = CreateFixture();
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.IssueToken(It.IsAny<User>())).Returns(("token", "jti", DateTime.UtcNow.AddHours(1)));
        var service = new AuthService(f.Context, tokens.Object, new Mock<ITokenBlocklistService>().Object, f.Publisher.Object);
        var saves = f.CountSaves();

        var response = await service.RegisterAsync(new RegisterRequest
        {
            Email = "samo.registrovan@test.ba", Password = "Lozinka123", ConfirmPassword = "Lozinka123",
            FirstName = "Samo", LastName = "Registrovan", PhoneNumber = "+387 61 123 456", VehicleCategoryId = 1,
        }, default);

        Assert.Equal(1, saves());
        Assert.NotNull(await f.Context.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == response.UserId));
        var charges = await f.Context.Charges.Where(c => c.StudentId == response.UserId).ToListAsync();
        Assert.Equal(3, charges.Count);
        Assert.Equal(1550m, charges.Sum(c => c.Amount));
    }
}
