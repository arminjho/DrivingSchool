using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Infrastructure.Payments;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Payments;

public class PackageChargeProvisionerTests
{
    [Fact]
    public async Task CreatesThreePackageChargesEachSplitIntoInstallmentsThatSumToThePrice()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var student = await f.Context.Users.FindAsync(PaymentTestFixture.StudentId);

        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, student!, default);
        await f.Context.SaveChangesAsync();

        var charges = await f.Context.Charges.Include(c => c.Installments).OrderBy(c => c.ServiceTypeId).ToListAsync();
        Assert.Equal([200m, 1200m, 150m], charges.Select(c => c.Amount));
        Assert.All(charges, c =>
        {
            Assert.Equal(PaymentTestFixture.StudentId, c.StudentId);
            Assert.Null(c.LessonId);
            Assert.Equal(ChargeStatus.Active, c.Status);
            Assert.Equal(PaymentConstants.PackageInstallmentCount, c.Installments.Count);
            Assert.Equal(c.Amount, c.Installments.Sum(i => i.Amount));
            Assert.All(c.Installments, i => Assert.Equal(InstallmentStatus.Pending, i.Status));
        });
        Assert.Equal(["Teorijska obuka (20h)", "Praktična vožnja (30h)", "Administracija i ispit"], charges.Select(c => c.Description));
    }

    [Fact]
    public async Task InstallmentsAreDueEveryThirtyDaysStartingThirtyDaysFromToday()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var student = await f.Context.Users.FindAsync(PaymentTestFixture.StudentId);

        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, student!, default);
        await f.Context.SaveChangesAsync();

        var dueDates = (await f.Context.Installments.Where(i => i.Charge!.ServiceTypeId == 11).OrderBy(i => i.Number).ToListAsync())
            .Select(i => i.DueDate).ToList();
        var today = DateTime.UtcNow.Date;
        Assert.Equal([today.AddDays(30), today.AddDays(60), today.AddDays(90)], dueDates);
    }

    [Fact]
    public async Task DoesNotCreateAChargeForTheExtraLessonPrice()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var student = await f.Context.Users.FindAsync(PaymentTestFixture.StudentId);

        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, student!, default);
        await f.Context.SaveChangesAsync();

        Assert.DoesNotContain(await f.Context.Charges.ToListAsync(), c => c.ServiceTypeId == 14);
    }

    [Fact]
    public async Task StagesChargesForAnUnsavedUserSoTheyAreStoredInTheSameSaveAsTheUser()
    {
        var f = new PaymentTestFixture();
        f.AddPriceList();
        var saves = f.CountSaves();
        var newStudent = new User
        {
            Email = "novi@test.ba", PasswordHash = "x", Role = "Student", FirstName = "Novi", LastName = "Učenik", PhoneNumber = "+387 61 111 222",
        };

        f.Context.Users.Add(newStudent);
        await PackageChargeProvisioner.AddPackageChargesAsync(f.Context, newStudent, default);

        Assert.Equal(0, await f.Context.Charges.CountAsync()); // nothing stored until the caller saves

        await f.Context.SaveChangesAsync();

        Assert.Equal(1, saves());
        Assert.NotEqual(0, newStudent.Id);
        var charges = await f.Context.Charges.ToListAsync();
        Assert.Equal(3, charges.Count);
        Assert.All(charges, c => Assert.Equal(newStudent.Id, c.StudentId));
    }
}
