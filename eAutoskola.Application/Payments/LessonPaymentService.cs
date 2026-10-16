using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Application.Payments;

/// <summary>The charge a lesson carries and what happens to it when the lesson is cancelled.</summary>
public record CancellationPaymentPlan(Charge Charge, Installment Installment, Payment? CompletedPayment);

public record RefundOutcome(decimal AmountKm);

/// <summary>
/// Payment side of the lesson lifecycle, called by LessonCommandService. Methods only stage
/// changes on the shared DbContext (the caller's single SaveChangesAsync commits them together
/// with the lesson transition) - the one exception is a failed refund, which is recorded on its
/// own because the caller then aborts.
/// </summary>
public class LessonPaymentService
{
    private readonly EAutoskolaDbContext _context;
    private readonly IPayPalClient _payPal;
    private readonly ILogger<LessonPaymentService> _logger;

    public LessonPaymentService(EAutoskolaDbContext context, IPayPalClient payPal, ILogger<LessonPaymentService> logger)
    {
        _context = context;
        _payPal = payPal;
        _logger = logger;
    }

    /// <summary>Stages an extra-lesson charge when this lesson goes beyond the package quota.
    /// Returns the price, or null when the lesson is covered by the package.</summary>
    public async Task<decimal?> AddExtraLessonChargeIfNeededAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        var otherLessons = await _context.Lessons.CountAsync(l =>
            l.StudentId == lesson.StudentId && l.Id != lesson.Id
            && (l.Status == LessonStatus.Confirmed || l.Status == LessonStatus.Completed), cancellationToken);
        if (!LessonPaymentPolicy.IsBeyondPackageQuota(otherLessons))
        {
            return null;
        }

        var serviceType = await _context.ServiceTypes.FirstOrDefaultAsync(s => s.Code == ServiceTypeCodes.ExtraLesson, cancellationToken)
            ?? throw new BusinessException(Messages.ExtraLessonPriceNotConfigured);

        _context.Charges.Add(new Charge
        {
            StudentId = lesson.StudentId,
            ServiceTypeId = serviceType.Id,
            LessonId = lesson.Id,
            Amount = serviceType.Price,
            Description = $"{serviceType.Name} ({lesson.StartsAtUtc:dd.MM.yyyy.})",
            Installments =
            [
                new Installment { Number = 1, Amount = serviceType.Price, DueDate = lesson.StartsAtUtc },
            ],
        });

        return serviceType.Price;
    }

    /// <summary>Loads the lesson's charge (if any) and enforces the student's refund cutoff.
    /// Nothing is modified, so it is safe to run before the caller's other validations finish.</summary>
    public async Task<CancellationPaymentPlan?> PrepareCancellationAsync(Lesson lesson, bool actorIsStudent, CancellationToken cancellationToken)
    {
        var charge = await _context.Charges
            .Include(c => c.Installments).ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(c => c.LessonId == lesson.Id && c.Status == ChargeStatus.Active, cancellationToken);
        if (charge is null)
        {
            return null;
        }

        var installment = charge.Installments.First();
        var completedPayment = installment.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Completed);

        if (completedPayment is not null && !LessonPaymentPolicy.IsRefundAllowed(actorIsStudent, lesson.StartsAtUtc, DateTime.UtcNow))
        {
            throw new BusinessException(string.Format(Messages.PaidLessonRefundCutoff, PaymentConstants.RefundCutoffHours));
        }

        return new CancellationPaymentPlan(charge, installment, completedPayment);
    }

    /// <summary>Unpaid: cancels the charge. Paid: refunds through PayPal FIRST, and only if
    /// PayPal accepted it stages the Refunded/Cancelled state. A failed refund is persisted as
    /// a Failed Refund row and aborts the cancellation.</summary>
    public async Task<RefundOutcome?> ApplyCancellationAsync(CancellationPaymentPlan? plan, Lesson lesson, CancellationToken cancellationToken)
    {
        if (plan is null)
        {
            return null;
        }

        var (charge, installment, payment) = plan;

        if (payment is null)
        {
            // An order the buyer opened but never paid must not be capturable after the lesson is gone.
            foreach (var openOrder in installment.Payments.Where(p => p.Status == PaymentStatus.Created))
            {
                openOrder.Status = PaymentStatus.Failed;
            }
            installment.Status = InstallmentStatus.Cancelled;
            charge.Status = ChargeStatus.Cancelled;
            return null;
        }

        var captured = payment.CapturedAmount ?? payment.PayPalAmount;
        PayPalRefundResult result;
        if (payment.PayPalCaptureId is null)
        {
            result = new PayPalRefundResult(false, null, "Payment has no PayPal capture id");
        }
        else
        {
            try
            {
                result = await _payPal.RefundCaptureAsync(payment.PayPalCaptureId, captured, payment.PayPalCurrency, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                result = new PayPalRefundResult(false, null, ex.Message);
            }
        }

        if (!result.Success)
        {
            _logger.LogError(
                "PayPal refund failed for lesson {LessonId}, payment {PaymentId}, capture {CaptureId}: {Reason}",
                lesson.Id, payment.Id, payment.PayPalCaptureId, result.FailureReason);
            await RecordFailedRefundAsync(payment, captured, cancellationToken);
            throw new BusinessException(Messages.RefundFailed);
        }

        payment.Status = PaymentStatus.Refunded;
        installment.Status = InstallmentStatus.Cancelled;
        charge.Status = ChargeStatus.Cancelled;
        _context.Refunds.Add(new Refund
        {
            PaymentId = payment.Id,
            Amount = captured,
            AmountKm = payment.Amount,
            PayPalRefundId = result.RefundId,
            Status = RefundStatus.Completed,
        });

        return new RefundOutcome(payment.Amount);
    }

    private async Task RecordFailedRefundAsync(Payment payment, decimal captured, CancellationToken cancellationToken)
    {
        _context.Refunds.Add(new Refund
        {
            PaymentId = payment.Id,
            Amount = captured,
            AmountKm = payment.Amount,
            Status = RefundStatus.Failed,
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Could not persist the failed refund record for payment {PaymentId}", payment.Id);
        }
    }
}
