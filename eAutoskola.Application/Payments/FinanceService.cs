using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Application.Payments;

/// <summary>Read side of the student's "Finansije i plaćanje" screen. Cancelled charges and
/// installments (refunded or cancelled lessons) are excluded from the breakdown and the
/// remaining balance; a refund shows up as its own line in the payment history.</summary>
public class FinanceService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(EAutoskolaDbContext context, ICurrentUserService currentUser, ILogger<FinanceService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<FinanceSummaryDto> GetMyAsync(CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;
        await EnsurePackageChargesAsync(studentId, cancellationToken);

        var charges = await _context.Charges.AsNoTracking()
            .Include(c => c.Installments)
            .Where(c => c.StudentId == studentId && c.Status == ChargeStatus.Active)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var pending = charges
            .SelectMany(c => c.Installments.Where(i => i.Status == InstallmentStatus.Pending).Select(i => (Charge: c, Installment: i)))
            .ToList();
        var next = pending.OrderBy(p => p.Installment.DueDate).ThenBy(p => p.Installment.Id).Cast<(Charge Charge, Installment Installment)?>().FirstOrDefault();

        return new FinanceSummaryDto
        {
            CostBreakdown = charges.Select(c => new CostBreakdownItemDto
            {
                ChargeId = c.Id,
                LessonId = c.LessonId,
                Description = c.Description,
                Amount = c.Amount,
                IsPaid = c.Installments.Count > 0 && c.Installments.All(i => i.Status == InstallmentStatus.Paid),
            }).ToList(),
            PaymentHistory = await BuildHistoryAsync(studentId, cancellationToken),
            RemainingBalance = pending.Sum(p => p.Installment.Amount),
            NextInstallment = next is { } n
                ? new NextInstallmentDto
                {
                    InstallmentId = n.Installment.Id,
                    Number = n.Installment.Number,
                    Description = n.Charge.Description,
                    Amount = n.Installment.Amount,
                    DueDate = n.Installment.DueDate,
                }
                : null,
        };
    }

    private async Task<List<PaymentHistoryItemDto>> BuildHistoryAsync(int studentId, CancellationToken cancellationToken)
    {
        // Refunded payments stay in the history as the payment they were; the refund is a
        // separate line below.
        var payments = await _context.Payments.AsNoTracking()
            .Where(p => p.Installment!.Charge!.StudentId == studentId
                && (p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Refunded))
            .Select(p => new
            {
                p.Amount,
                p.CompletedAt,
                p.CreatedAt,
                ChargeDescription = p.Installment!.Charge!.Description,
                p.Installment.Number,
                InstallmentCount = p.Installment.Charge.Installments.Count,
            })
            .ToListAsync(cancellationToken);

        var refunds = await _context.Refunds.AsNoTracking()
            .Where(r => r.Payment!.Installment!.Charge!.StudentId == studentId && r.Status == RefundStatus.Completed)
            .Select(r => new { r.AmountKm, r.CreatedAt })
            .ToListAsync(cancellationToken);

        var history = payments
            .Select(p => new PaymentHistoryItemDto
            {
                Type = "Payment",
                Label = p.InstallmentCount > 1 ? $"{p.ChargeDescription} - rata {p.Number}" : p.ChargeDescription,
                Amount = p.Amount,
                DateUtc = p.CompletedAt ?? p.CreatedAt,
            })
            .Concat(refunds.Select(r => new PaymentHistoryItemDto
            {
                Type = "Refund",
                Label = Messages.RefundLabel,
                Amount = r.AmountKm,
                DateUtc = r.CreatedAt,
            }));

        return history.OrderBy(h => h.DateUtc).ToList();
    }

    /// <summary>Package charges (theory, practical, administration) are created lazily the
    /// first time a student opens the finance screen, so every way a student account comes
    /// into existence (self-registration, admin-created, seeded) is covered without touching
    /// those code paths.</summary>
    private async Task EnsurePackageChargesAsync(int studentId, CancellationToken cancellationToken)
    {
        var existingServiceTypeIds = await _context.Charges
            .Where(c => c.StudentId == studentId && c.LessonId == null)
            .Select(c => c.ServiceTypeId)
            .ToListAsync(cancellationToken);

        var missing = await _context.ServiceTypes
            .Where(s => ServiceTypeCodes.Package.Contains(s.Code) && !existingServiceTypeIds.Contains(s.Id))
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);
        if (missing.Count == 0)
        {
            return;
        }

        var createdAt = DateTime.UtcNow;
        foreach (var serviceType in missing)
        {
            var amounts = InstallmentPlanner.Split(serviceType.Price, PaymentConstants.PackageInstallmentCount);
            _context.Charges.Add(new Charge
            {
                StudentId = studentId,
                ServiceTypeId = serviceType.Id,
                Amount = serviceType.Price,
                Description = serviceType.Name,
                CreatedAt = createdAt,
                Installments = amounts.Select((amount, index) => new Installment
                {
                    Number = index + 1,
                    Amount = amount,
                    DueDate = createdAt.Date.AddDays(PaymentConstants.InstallmentIntervalDays * (index + 1)),
                    CreatedAt = createdAt,
                }).ToList(),
            });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // A concurrent request provisioned the same charges first (unique index) - discard
            // our attempt and read what it created.
            _logger.LogInformation(ex, "Package charges for student {StudentId} were created concurrently", studentId);
            _context.ChangeTracker.Clear();
        }
    }
}
