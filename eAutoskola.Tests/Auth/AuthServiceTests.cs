using eAutoskola.Application.Auth;
using eAutoskola.Application.Auth.Requests;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Security;
using eAutoskola.Tests.Payments;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace eAutoskola.Tests.Auth;

/// <summary>Dorada #15: a still-unused reset code must stop working the moment a newer one is
/// issued, and every outstanding code must stop working the moment the password is actually
/// reset - otherwise an earlier code quietly remains able to reset the password again within
/// its own 15-minute window.</summary>
public class AuthServiceTests
{
    private static (AuthService Service, PaymentTestFixture F) Create()
    {
        var f = new PaymentTestFixture();
        var service = new AuthService(f.Context, new Mock<ITokenService>().Object, new Mock<ITokenBlocklistService>().Object, f.Publisher.Object);
        return (service, f);
    }

    private static ResetPasswordRequest NewPasswordRequest(string email, string code) => new()
    {
        Email = email, Code = code, NewPassword = "NovaLozinka123", ConfirmNewPassword = "NovaLozinka123",
    };

    /// <summary>Captures the plaintext code from the published e-mail - AuthService only ever
    /// stores its hash, so this is the sole way a test can get a real code back out.</summary>
    private static async Task<string> RequestCode(AuthService service, PaymentTestFixture f, string email)
    {
        string? captured = null;
        f.Publisher
            .Setup(p => p.PublishAsync(It.IsAny<PasswordResetRequestedMessage>(), It.IsAny<CancellationToken>()))
            .Callback<PasswordResetRequestedMessage, CancellationToken>((message, _) => captured = message.Code)
            .ReturnsAsync(true);

        await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = email }, default);
        return captured!;
    }

    [Fact]
    public async Task RequestingASecondCode_InvalidatesTheFirst()
    {
        var (service, f) = Create();
        var user = f.Context.Users.Single(u => u.Id == PaymentTestFixture.StudentId);

        await RequestCode(service, f, user.Email);
        await RequestCode(service, f, user.Email);

        var tokens = await f.Context.PasswordResetTokens.Where(t => t.UserId == user.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Equal(1, tokens.Count(t => !t.IsUsed));
    }

    [Fact]
    public async Task ResettingWithAnOlderCode_ThatWasSupersededByANewerOne_IsRejected()
    {
        var (service, f) = Create();
        var user = f.Context.Users.Single(u => u.Id == PaymentTestFixture.StudentId);

        var firstCode = await RequestCode(service, f, user.Email);
        await RequestCode(service, f, user.Email);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ResetPasswordAsync(NewPasswordRequest(user.Email, firstCode), default));

        Assert.Contains(nameof(ResetPasswordRequest.Code), ex.Errors.Keys);
    }

    [Fact]
    public async Task ResettingWithTheLatestCode_ThenReusingItAgain_IsRejectedTheSecondTime()
    {
        var (service, f) = Create();
        var user = f.Context.Users.Single(u => u.Id == PaymentTestFixture.StudentId);
        var code = await RequestCode(service, f, user.Email);
        var request = NewPasswordRequest(user.Email, code);

        await service.ResetPasswordAsync(request, default);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.ResetPasswordAsync(request, default));
        Assert.Contains(nameof(ResetPasswordRequest.Code), ex.Errors.Keys);
    }

    [Fact]
    public async Task ResettingThePassword_InvalidatesAnyOtherOutstandingCodeToo()
    {
        var (service, f) = Create();
        var user = f.Context.Users.Single(u => u.Id == PaymentTestFixture.StudentId);
        var code = await RequestCode(service, f, user.Email);

        // Added AFTER the request above (so step (a)'s own "invalidate previous codes" cleanup
        // never had a chance to touch it - only ResetPasswordAsync's own cleanup can catch this),
        // but backdated so it is not the one ResetPasswordAsync's "latest unused" lookup selects.
        f.Context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id, CodeHash = PasswordHasher.Hash("999999"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        });
        await f.Context.SaveChangesAsync();

        await service.ResetPasswordAsync(NewPasswordRequest(user.Email, code), default);

        Assert.True(await f.Context.PasswordResetTokens.AllAsync(t => t.UserId != user.Id || t.IsUsed));
    }
}
