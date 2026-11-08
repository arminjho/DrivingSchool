using eAutoskola.Application.Me;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Files;
using eAutoskola.Domain.Security;
using eAutoskola.Tests.Payments;
using Mapster;
using Moq;

namespace eAutoskola.Tests.Me;

/// <summary>Dorada #14: seed/demo accounts (desktop/mobile/instruktor) log in with a short word
/// in the Email column, since login is by that same column. Enforcing the e-mail format on
/// every profile save blocked ANY field from being saved - and would have changed the login -
/// until that word was replaced with a real address.</summary>
public class MeServiceTests
{
    static MeServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(MeService).Assembly);

    private static (MeService Service, PaymentTestFixture F) Create(string email)
    {
        var f = new PaymentTestFixture();
        var user = f.Context.Users.Single(u => u.Id == PaymentTestFixture.StudentId);
        user.Email = email;
        f.Context.SaveChanges();

        var service = new MeService(f.Context, f.CurrentUser.Object, new Mock<ITokenBlocklistService>().Object, new Mock<IFileStorageService>().Object);
        return (service, f);
    }

    private static MeUpdateRequest Request(string email, string? address = null) => new()
    {
        FirstName = "Ime", LastName = "Prezime", Email = email, PhoneNumber = "+387 61 123 456", Address = address,
    };

    [Fact]
    public async Task ADemoAccount_WithANonEmailLogin_CanStillSaveOtherFields()
    {
        var (service, _) = Create("mobile");

        var dto = await service.UpdateMeAsync(Request("mobile", address: "Nova adresa 1"), default);

        Assert.Equal("mobile", dto.Email);
        Assert.Equal("Nova adresa 1", dto.Address);
    }

    [Fact]
    public async Task ADemoAccount_ActuallyChangingTheEmail_StillNeedsAValidFormat()
    {
        var (service, _) = Create("mobile");

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateMeAsync(Request("not-an-email"), default));

        Assert.Contains("formatu", ex.Errors[nameof(MeUpdateRequest.Email)][0]);
    }

    [Fact]
    public async Task ADemoAccount_ChangingToARealAddress_Succeeds()
    {
        var (service, _) = Create("mobile");

        var dto = await service.UpdateMeAsync(Request("mobile.ucenik@test.ba"), default);

        Assert.Equal("mobile.ucenik@test.ba", dto.Email);
    }

    [Fact]
    public async Task ADemoAccount_ChangingToAnAlreadyUsedEmail_IsStillRejected()
    {
        var (service, f) = Create("mobile");
        f.Context.Users.Add(new() { Id = 99, Email = "taken@test.ba", PasswordHash = "x", Role = Role.Student, FirstName = "X", LastName = "Y", PhoneNumber = "+387 61 000 000" });
        f.Context.SaveChanges();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateMeAsync(Request("taken@test.ba"), default));

        Assert.Contains(nameof(MeUpdateRequest.Email), ex.Errors.Keys);
    }
}
