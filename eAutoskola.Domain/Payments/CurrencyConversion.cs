namespace eAutoskola.Domain.Payments;

/// <summary>KM -> EUR at the fixed currency-board rate, used only when talking to PayPal.</summary>
public static class CurrencyConversion
{
    public static decimal KmToEur(decimal amountKm)
        => Math.Round(amountKm / PaymentConstants.BamPerEur, 2, MidpointRounding.AwayFromZero);
}
