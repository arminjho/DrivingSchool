using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Payments;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Infrastructure.Payments;

/// <summary>
/// The one place a student's package charges (theory, practical driving, administration/exam,
/// each split into installments) are created. Every way a student comes into existence calls
/// it - self-registration, admin "Novi korisnik", and the seeder - and it only STAGES entities
/// on the context, so the charges are saved in the same SaveChangesAsync (one transaction) as
/// the user and profile. Reading the finance screen never creates anything.
/// </summary>
public static class PackageChargeProvisioner
{
    public static async Task AddPackageChargesAsync(EAutoskolaDbContext context, User student, CancellationToken cancellationToken)
    {
        var packageServiceTypes = await context.ServiceTypes
            .Where(s => ServiceTypeCodes.Package.Contains(s.Code))
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var createdAtUtc = DateTime.UtcNow;
        foreach (var serviceType in packageServiceTypes)
        {
            var amounts = InstallmentPlanner.Split(serviceType.Price, PaymentConstants.PackageInstallmentCount);

            // Linked through the Student navigation so it also works for a user that has no Id
            // yet (not saved).
            context.Charges.Add(new Charge
            {
                Student = student,
                ServiceTypeId = serviceType.Id,
                Amount = serviceType.Price,
                Description = serviceType.Name,
                CreatedAt = createdAtUtc,
                Installments = amounts.Select((amount, index) => new Installment
                {
                    Number = index + 1,
                    Amount = amount,
                    DueDate = createdAtUtc.Date.AddDays(PaymentConstants.InstallmentIntervalDays * (index + 1)),
                    CreatedAt = createdAtUtc,
                }).ToList(),
            });
        }
    }
}
