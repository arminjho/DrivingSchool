namespace eAutoskola.Application.Recommendations;

/// <summary>Pure, unit-testable vector math - no I/O, no DbContext. Vectors are sparse,
/// represented as QuestionAreaId -> weight dictionaries (docs §6.2 "vektori").</summary>
public static class VectorMath
{
    /// <summary>cos(θ) = (A·B) / (|A| × |B|). Returns 0 if either vector has zero
    /// magnitude (no shared or no weighted dimensions at all).</summary>
    public static double CosineSimilarity(IReadOnlyDictionary<int, double> a, IReadOnlyDictionary<int, double> b)
    {
        double dot = 0;
        foreach (var (dimension, weight) in a)
        {
            if (b.TryGetValue(dimension, out var otherWeight))
            {
                dot += weight * otherWeight;
            }
        }

        var magnitudeA = Math.Sqrt(a.Values.Sum(v => v * v));
        var magnitudeB = Math.Sqrt(b.Values.Sum(v => v * v));

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dot / (magnitudeA * magnitudeB);
    }
}
