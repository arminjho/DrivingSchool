using eAutoskola.Application.Recommendations;

namespace eAutoskola.Tests.Recommendations;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var a = new Dictionary<int, double> { [1] = 0.8, [2] = 0.2 };
        var b = new Dictionary<int, double> { [1] = 0.8, [2] = 0.2 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(1.0, result, precision: 10);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = new Dictionary<int, double> { [1] = 1.0 };
        var b = new Dictionary<int, double> { [2] = 1.0 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result, precision: 10);
    }

    [Fact]
    public void CosineSimilarity_PartialOverlap_ReturnsValueBetweenZeroAndOne()
    {
        var a = new Dictionary<int, double> { [1] = 1.0, [2] = 0.3 };
        var b = new Dictionary<int, double> { [1] = 1.0 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.True(result > 0 && result < 1);
    }

    [Fact]
    public void CosineSimilarity_ZeroMagnitudeVector_ReturnsZero()
    {
        var a = new Dictionary<int, double>();
        var b = new Dictionary<int, double> { [1] = 1.0 };

        var result = VectorMath.CosineSimilarity(a, b);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CosineSimilarity_IsSymmetric()
    {
        var a = new Dictionary<int, double> { [1] = 0.6, [2] = 0.8 };
        var b = new Dictionary<int, double> { [1] = 0.2, [3] = 0.9 };

        Assert.Equal(VectorMath.CosineSimilarity(a, b), VectorMath.CosineSimilarity(b, a), precision: 10);
    }
}
