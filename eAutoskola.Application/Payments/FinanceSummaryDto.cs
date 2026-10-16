namespace eAutoskola.Application.Payments;

/// <summary>Everything the "Finansije i plaćanje" screen shows. All amounts are in KM.</summary>
public class FinanceSummaryDto
{
    public List<CostBreakdownItemDto> CostBreakdown { get; set; } = [];
    public List<PaymentHistoryItemDto> PaymentHistory { get; set; } = [];
    public decimal RemainingBalance { get; set; }
    public NextInstallmentDto? NextInstallment { get; set; }
}

public class CostBreakdownItemDto
{
    public int ChargeId { get; set; }
    public int? LessonId { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
}

public class PaymentHistoryItemDto
{
    /// <summary>"Payment" or "Refund".</summary>
    public required string Type { get; set; }

    /// <summary>The paid installment's label (e.g. "Praktična vožnja (30h) - rata 2"), or "Povrat".</summary>
    public required string Label { get; set; }

    public decimal Amount { get; set; }
    public DateTime DateUtc { get; set; }
}

public class NextInstallmentDto
{
    public int InstallmentId { get; set; }
    public int Number { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}
