using System.Text.Json.Serialization;

namespace ShamirSecretSharing.Core;

public sealed class ShamirShare
{
    [JsonConstructor]
    public ShamirShare(int version, int prime, int threshold, int x, ushort[] yValues, int secretLength)
    {
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be greater than zero.");
        }

        if (prime <= 2)
        {
            throw new ArgumentOutOfRangeException(nameof(prime), "Prime must be greater than 2.");
        }

        if (threshold < 2 || threshold >= prime)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be at least 2 and less than the prime.");
        }

        if (x <= 0 || x >= prime)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "x must be between 1 and prime - 1.");
        }

        if (secretLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(secretLength), "Secret length must be greater than zero.");
        }

        if (yValues is null)
        {
            throw new ArgumentNullException(nameof(yValues));
        }

        if (yValues.Length != secretLength)
        {
            throw new ArgumentException("yValues length must match secretLength.", nameof(yValues));
        }

        for (var i = 0; i < yValues.Length; i++)
        {
            if (yValues[i] >= prime)
            {
                throw new ArgumentOutOfRangeException(nameof(yValues), $"yValues[{i}] must be less than the prime.");
            }
        }

        Version = version;
        Prime = prime;
        Threshold = threshold;
        X = x;
        SecretLength = secretLength;
        YValues = yValues.ToArray();
    }

    public ShamirShare(int threshold, int x, ushort[] yValues, int secretLength)
        : this(CurrentVersion, CurrentPrime, threshold, x, yValues, secretLength)
    {
    }

    public const int CurrentVersion = 1;
    public const int CurrentPrime = Gf257.Prime;

    public int Version { get; }
    public int Prime { get; }
    public int Threshold { get; }
    public int X { get; }
    public int SecretLength { get; }
    public ushort[] YValues { get; }
}
