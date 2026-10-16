namespace eAutoskola.Domain.Payments;

public static class InstallmentPlanner
{
    /// <summary>Splits a total into equal 2-decimal parts; the last part absorbs the rounding
    /// remainder so the parts always sum to exactly the total.</summary>
    public static decimal[] Split(decimal total, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var part = Math.Round(total / count, 2, MidpointRounding.AwayFromZero);
        var parts = Enumerable.Repeat(part, count).ToArray();
        parts[^1] = total - part * (count - 1);
        return parts;
    }
}
