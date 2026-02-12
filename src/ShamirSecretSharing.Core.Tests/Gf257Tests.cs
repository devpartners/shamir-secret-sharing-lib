using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class Gf257Tests
{
    // ── Normalize ──────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(256, 256)]
    [InlineData(257, 0)]
    [InlineData(258, 1)]
    [InlineData(514, 0)]
    [InlineData(-1, 256)]
    [InlineData(-257, 0)]
    [InlineData(-258, 256)]
    public void Normalize_ReturnsExpected(int input, int expected)
    {
        Assert.Equal(expected, Gf257.Normalize(input));
    }

    // ── Add ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(100, 50, 150)]
    [InlineData(200, 100, 43)]   // 300 % 257 = 43
    [InlineData(256, 1, 0)]      // 257 % 257 = 0
    [InlineData(0, 256, 256)]
    public void Add_ReturnsExpected(int left, int right, int expected)
    {
        Assert.Equal(expected, Gf257.Add(left, right));
    }

    // ── Sub ────────────────────────────────────────────────────

    [Theory]
    [InlineData(100, 50, 50)]
    [InlineData(50, 100, 207)]   // -50 mod 257 = 207
    [InlineData(0, 1, 256)]
    [InlineData(0, 0, 0)]
    [InlineData(256, 256, 0)]
    public void Sub_ReturnsExpected(int left, int right, int expected)
    {
        Assert.Equal(expected, Gf257.Sub(left, right));
    }

    // ── Mul ────────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(1, 100, 100)]
    [InlineData(100, 1, 100)]
    [InlineData(16, 16, 256)]     // 256 % 257 = 256
    [InlineData(16, 17, 15)]      // 272 % 257 = 15
    [InlineData(256, 256, 1)]     // 256*256 = 65536, 65536 % 257 = 1
    public void Mul_ReturnsExpected(int left, int right, int expected)
    {
        Assert.Equal(expected, Gf257.Mul(left, right));
    }

    // ── Div ────────────────────────────────────────────────────

    [Fact]
    public void Div_ZeroNumerator_ReturnsZero()
    {
        Assert.Equal(0, Gf257.Div(0, 1));
    }

    [Fact]
    public void Div_ByOne_ReturnsSameValue()
    {
        Assert.Equal(42, Gf257.Div(42, 1));
    }

    [Fact]
    public void Div_SameValue_ReturnsOne()
    {
        Assert.Equal(1, Gf257.Div(100, 100));
    }

    [Fact]
    public void Div_ByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Gf257.Div(42, 0));
    }

    [Fact]
    public void Div_RoundTripsWithMul()
    {
        // a / b * b == a
        var a = 123;
        var b = 45;
        var quotient = Gf257.Div(a, b);
        Assert.Equal(a, Gf257.Mul(quotient, b));
    }

    // ── Pow ────────────────────────────────────────────────────

    [Fact]
    public void Pow_ExponentZero_ReturnsOne()
    {
        Assert.Equal(1, Gf257.Pow(42, 0));
    }

    [Fact]
    public void Pow_ExponentOne_ReturnsNormalizedValue()
    {
        Assert.Equal(42, Gf257.Pow(42, 1));
    }

    [Theory]
    [InlineData(2, 8, 256)]    // 256 % 257 = 256
    [InlineData(2, 9, 255)]    // 512 % 257 = 255
    public void Pow_ReturnsExpected(int value, int exponent, int expected)
    {
        Assert.Equal(expected, Gf257.Pow(value, exponent));
    }

    [Fact]
    public void Pow_NegativeExponent_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Gf257.Pow(2, -1));
    }

    [Fact]
    public void Pow_FermatsLittleTheorem()
    {
        // a^(p-1) ≡ 1 (mod p) for a != 0
        for (var a = 1; a < 257; a++)
        {
            Assert.Equal(1, Gf257.Pow(a, 256));
        }
    }

    // ── Inv ────────────────────────────────────────────────────

    [Fact]
    public void Inv_Zero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => Gf257.Inv(0));
    }

    [Fact]
    public void Inv_One_ReturnsOne()
    {
        Assert.Equal(1, Gf257.Inv(1));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(128)]
    [InlineData(256)]
    public void Inv_MulByInverse_ReturnsOne(int value)
    {
        Assert.Equal(1, Gf257.Mul(value, Gf257.Inv(value)));
    }

    // ── EvaluatePolynomial ─────────────────────────────────────

    [Fact]
    public void EvaluatePolynomial_SingleCoefficient_ReturnsConstant()
    {
        // p(x) = 42
        Assert.Equal(42, Gf257.EvaluatePolynomial([42], 0));
        Assert.Equal(42, Gf257.EvaluatePolynomial([42], 5));
        Assert.Equal(42, Gf257.EvaluatePolynomial([42], 256));
    }

    [Fact]
    public void EvaluatePolynomial_Linear()
    {
        // p(x) = 3 + 5x
        // p(0) = 3, p(1) = 8, p(2) = 13
        Assert.Equal(3, Gf257.EvaluatePolynomial([3, 5], 0));
        Assert.Equal(8, Gf257.EvaluatePolynomial([3, 5], 1));
        Assert.Equal(13, Gf257.EvaluatePolynomial([3, 5], 2));
    }

    [Fact]
    public void EvaluatePolynomial_Quadratic()
    {
        // p(x) = 1 + 2x + 3x^2
        // p(0) = 1, p(1) = 6, p(2) = 1+4+12 = 17
        Assert.Equal(1, Gf257.EvaluatePolynomial([1, 2, 3], 0));
        Assert.Equal(6, Gf257.EvaluatePolynomial([1, 2, 3], 1));
        Assert.Equal(17, Gf257.EvaluatePolynomial([1, 2, 3], 2));
    }

    [Fact]
    public void EvaluatePolynomial_EmptyCoefficients_Throws()
    {
        Assert.Throws<ArgumentException>(() => Gf257.EvaluatePolynomial([], 1));
    }

    [Fact]
    public void EvaluatePolynomial_ResultWrapsModPrime()
    {
        // p(x) = 200 + 200x  →  p(1) = 400 % 257 = 143
        Assert.Equal(143, Gf257.EvaluatePolynomial([200, 200], 1));
    }

    // ── InterpolateAtZero ──────────────────────────────────────

    [Fact]
    public void InterpolateAtZero_TwoPoints_ReconstructsConstantTerm()
    {
        // p(x) = 5 + 3x  →  p(1)=8, p(2)=11  →  interpolate back to p(0)=5
        int[] xs = [1, 2];
        int[] ys = [8, 11];
        Assert.Equal(5, Gf257.InterpolateAtZero(xs, ys));
    }

    [Fact]
    public void InterpolateAtZero_ThreePoints_ReconstructsConstantTerm()
    {
        // p(x) = 10 + 2x + x^2  →  p(1)=13, p(2)=18, p(3)=25
        int[] xs = [1, 2, 3];
        int[] ys = [13, 18, 25];
        Assert.Equal(10, Gf257.InterpolateAtZero(xs, ys));
    }

    [Fact]
    public void InterpolateAtZero_MismatchedLengths_Throws()
    {
        Assert.Throws<ArgumentException>(() => Gf257.InterpolateAtZero([1, 2], [10]));
    }

    [Fact]
    public void InterpolateAtZero_EmptyArrays_Throws()
    {
        Assert.Throws<ArgumentException>(() => Gf257.InterpolateAtZero([], []));
    }

    [Fact]
    public void InterpolateAtZero_SinglePoint_ReturnsYValue()
    {
        // With one point (x=3, y=42), the constant polynomial is 42
        // But actually for a single point, the Lagrange basis at x=0 is: L_0(0) = 1 (only one term)
        // Wait - for single point, basis = 1, so result = y * 1 = y
        // Actually let me re-check: for single point (x0, y0), the interpolation is y0 * (0 - x0)/(x0 - x0)?
        // No - for single point, there's no denominator loop (j != i skips everything),
        // so numerator=1, denominator=1, basis=1, result=y0
        Assert.Equal(42, Gf257.InterpolateAtZero([3], [42]));
    }
}
