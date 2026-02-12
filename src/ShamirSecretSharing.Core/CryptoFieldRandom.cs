using System.Security.Cryptography;

namespace ShamirSecretSharing.Core;

public sealed class CryptoFieldRandom : IFieldRandom
{
    public int NextInt(int maxExclusive)
    {
        return RandomNumberGenerator.GetInt32(maxExclusive);
    }
}
