using eAutoskola.Application.Notifications;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Messaging;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eAutoskola.Application.Payments;

/// <summary>
/// PayPal checkout for one installment. A payment is only ever finalized by
/// <see cref="CaptureAsync"/>, which asks PayPal itself whether the buyer approved and paid -
/// nothing the client sends is trusted as proof of payment (upute.md §7.1).
/// </summary>
public class PaymentService
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPayPalClient _payPal;
    private readonly NotificationService _notificationService;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        EAutoskolaDbContext context,
        ICurrentUserService currentUser,
        IPayPalClient payPal,
        NotificationService notificationService,
        IMessagePublisher messagePublisher,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _payPal = payPal;
        _notificationService = notificationService;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<CreateOrderResultDto> CreateOrderAsync(int installmentId, CancellationToken cancellationToken)
    {
        var installment = await _context.Installments
            .Include(i => i.Charge)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == installmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Installment), installmentId);
        var charge = installment.Charge!;

        if (charge.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
        if (installment.Status == InstallmentStatus.Paid || installment.Payments.Any(p => p.Status == PaymentStatus.Completed))
        {
            throw new BusinessException(Messages.InstallmentAlreadyPaid);
        }
        if (installment.Status != InstallmentStatus.Pending || charge.Status != ChargeStatus.Active)
        {
            throw new BusinessException(Messages.InstallmentNotPayable);
        }

        // An order the buyer abandoned is reused while it is still fresh (same OrderId and
        // ApprovalUrl) rather than blocking a retry or piling up orders.
        var nowUtc = DateTime.UtcNow;
        var openOrders = installment.Payments
            .Where(p => p.Status == PaymentStatus.Created)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();
        var reusable = openOrders.FirstOrDefault(p => LessonPaymentPolicy.IsPendingOrderReusable(p.CreatedAt, nowUtc));
        if (reusable is not null)
        {
            return ToOrderResult(reusable);
        }
        foreach (var stale in openOrders)
        {
            stale.Status = PaymentStatus.Failed;
        }

        var amountEur = CurrencyConversion.KmToEur(installment.Amount);
        PayPalOrderResult order;
        try
        {
            order = await _payPal.CreateOrderAsync(amountEur, $"eAutoškola - {charge.Description}", cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Creating a PayPal order failed for installment {InstallmentId}", installment.Id);
            throw new BusinessException(Messages.PaymentUnavailable, ex);
        }

        var payment = new Payment
        {
            InstallmentId = installment.Id,
            Amount = installment.Amount,
            PayPalAmount = amountEur,
            PayPalCurrency = _payPal.Currency,
            PayPalOrderId = order.OrderId,
            PayPalApprovalUrl = order.ApprovalUrl,
            Status = PaymentStatus.Created,
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return ToOrderResult(payment);
    }

    public async Task<CapturePaymentResultDto> CaptureAsync(string orderId, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .Include(p => p.Installment!).ThenInclude(i => i.Charge)
            .FirstOrDefaultAsync(p => p.PayPalOrderId == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), orderId);
        var installment = payment.Installment!;
        var charge = installment.Charge!;

        if (charge.StudentId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }

        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            return ToCaptureResult(payment, alreadyProcessed: true);
        }
        if (payment.Status == PaymentStatus.Failed)
        {
            throw new BusinessException(Messages.PaymentOrderExpired);
        }
        if (installment.Status == InstallmentStatus.Paid)
        {
            throw new BusinessException(Messages.InstallmentAlreadyPaid);
        }
        if (installment.Status != InstallmentStatus.Pending || charge.Status != ChargeStatus.Active)
        {
            throw new BusinessException(Messages.InstallmentNotPayable);
        }

        PayPalCaptureResult capture;
        try
        {
            capture = await _payPal.CaptureOrderAsync(orderId, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Capturing PayPal order {OrderId} failed", orderId);
            throw new BusinessException(Messages.PaymentUnavailable, ex);
        }

        if (!capture.Success)
        {
            // A parallel request may have finalized this payment while PayPal was being asked.
            await _context.Entry(payment).ReloadAsync(cancellationToken);
            if (payment.Status == PaymentStatus.Completed)
            {
                return ToCaptureResult(payment, alreadyProcessed: true);
            }

            _logger.LogInformation("PayPal order {OrderId} is not captured yet: {Reason}", orderId, capture.FailureReason);
            throw new BusinessException(Messages.PaymentNotCompleted);
        }

        payment.Status = PaymentStatus.Completed;
        payment.CapturedAmount = capture.CapturedAmount;
        payment.PayPalCaptureId = capture.CaptureId;
        payment.CompletedAt = DateTime.UtcNow;
        installment.Status = InstallmentStatus.Paid;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Lost the race against a parallel capture of the same order: it already applied
            // the first-capture side effects, so this call must not repeat them.
            await _context.Entry(payment).ReloadAsync(cancellationToken);
            if (payment.Status == PaymentStatus.Completed)
            {
                return ToCaptureResult(payment, alreadyProcessed: true);
            }
            throw;
        }

        await SendPaymentReceivedAsync(charge.StudentId, payment, cancellationToken);
        return ToCaptureResult(payment, alreadyProcessed: false);
    }

    // The money has already moved at this point, so a notification/e-mail problem must never
    // turn a successful capture into an error response.
    private async Task SendPaymentReceivedAsync(int studentId, Payment payment, CancellationToken cancellationToken)
    {
        try
        {
            var student = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == studentId, cancellationToken);
            await _notificationService.NotifyAsync(studentId, "Uplata primljena",
                $"Vaša uplata u iznosu od {payment.Amount:F2} KM je uspješno evidentirana.",
                NotificationType.PaymentReceived, cancellationToken);
            await _messagePublisher.PublishAsync(new PaymentReceivedMessage
            {
                ToEmail = student.Email,
                ToName = $"{student.FirstName} {student.LastName}",
                Amount = payment.Amount,
            }, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Payment {PaymentId} was captured but its notification/e-mail could not be sent", payment.Id);
        }
    }

    private static CreateOrderResultDto ToOrderResult(Payment payment) => new()
    {
        PaymentId = payment.Id,
        OrderId = payment.PayPalOrderId,
        ApprovalUrl = payment.PayPalApprovalUrl,
        Amount = payment.Amount,
        PayPalAmount = payment.PayPalAmount,
        PayPalCurrency = payment.PayPalCurrency,
    };

    private static CapturePaymentResultDto ToCaptureResult(Payment payment, bool alreadyProcessed) => new()
    {
        PaymentId = payment.Id,
        InstallmentId = payment.InstallmentId,
        Status = payment.Status.ToString(),
        Amount = payment.Amount,
        AlreadyProcessed = alreadyProcessed,
    };
}
