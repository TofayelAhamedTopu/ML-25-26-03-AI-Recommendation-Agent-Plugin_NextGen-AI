using AIRecommendation;
using Xunit;

public sealed class VectorMathTests
{
    [Fact]
    public void Cosine_SameVector_IsOne()
    {
        var m = new VectorMath();
        var a = new float[] { 1, 2, 3 };
        var sim = m.CosineSimilarity(a, a);
        Assert.True(sim > 0.999);
    }

    [Fact]
    public void Cosine_Orthogonal_IsZero()
    {
        var m = new VectorMath();
        var a = new float[] { 1, 0 };
        var b = new float[] { 0, 1 };
        var sim = m.CosineSimilarity(a, b);
        Assert.True(Math.Abs(sim) < 1e-6);
    }
}
