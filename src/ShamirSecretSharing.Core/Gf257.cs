namespace ShamirSecretSharing.Core;

internal static class Gf257
{
    public const int Prime = 257;

    public static int Add(int left, int right)
    {
        return Normalize(left + right);
    }

    public static int Sub(int left, int right)
    {
        return Normalize(left - right);
    }

    public static int Mul(int left, int right)
    {
        var product = (long)Normalize(left) * Normalize(right);
        return (int)(product % Prime);
    }

    public static int Div(int numerator, int denominator)
    {
        return Mul(numerator, Inv(denominator));
    }

    public static int Pow(int value, int exponent)
    {
        if (exponent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent must be non-negative.");
        }

        var result = 1;
        var factor = Normalize(value);
        var remainingExponent = exponent;

        while (remainingExponent > 0)
        {
            if ((remainingExponent & 1) == 1)
            {
                result = Mul(result, factor);
            }

            factor = Mul(factor, factor);
            remainingExponent >>= 1;
        }

        return result;
    }

    public static int Inv(int value)
    {
        var normalized = Normalize(value);
        if (normalized == 0)
        {
            throw new DivideByZeroException("Zero does not have a multiplicative inverse in GF(257).");
        }

        return Pow(normalized, Prime - 2);
    }

    public static int EvaluatePolynomial(ReadOnlySpan<int> coefficients, int x)
    {
        if (coefficients.IsEmpty)
        {
            throw new ArgumentException("At least one coefficient is required.", nameof(coefficients));
        }

        var result = 0;
        var normalizedX = Normalize(x);

        for (var i = coefficients.Length - 1; i >= 0; i--)
        {
            result = Add(Mul(result, normalizedX), coefficients[i]);
        }

        return result;
    }

    public static int InterpolateAtZero(ReadOnlySpan<int> xValues, ReadOnlySpan<int> yValues)
    {
        if (xValues.Length != yValues.Length)
        {
            throw new ArgumentException("xValues and yValues lengths must match.");
        }

        if (xValues.IsEmpty)
        {
            throw new ArgumentException("At least one point is required.");
        }

        var result = 0;

        for (var i = 0; i < xValues.Length; i++)
        {
            var numerator = 1;
            var denominator = 1;
            var xi = Normalize(xValues[i]);

            for (var j = 0; j < xValues.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var xj = Normalize(xValues[j]);
                numerator = Mul(numerator, Sub(0, xj));
                denominator = Mul(denominator, Sub(xi, xj));
            }

            var basisAtZero = Div(numerator, denominator);
            result = Add(result, Mul(yValues[i], basisAtZero));
        }

        return result;
    }

    public static int Normalize(int value)
    {
        var normalized = value % Prime;
        return normalized < 0 ? normalized + Prime : normalized;
    }
}
