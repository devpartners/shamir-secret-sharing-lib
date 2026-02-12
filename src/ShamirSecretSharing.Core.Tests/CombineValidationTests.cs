using System.Text;
using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class CombineValidationTests
{
    [Fact]
    public void Combine_NullShares_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShamirSecretSharer.Combine(null!));
    }

    [Fact]
    public void Combine_NullElementInShares_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("abc"), shareCount: 3, threshold: 2);

        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine([shares[0], null!, shares[2]]));

        Assert.Contains("null share", ex.Message);
    }

    [Fact]
    public void Combine_EmptyShareCollection_ThrowsValidationException()
    {
        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine(Array.Empty<ShamirShare>()));

        Assert.Contains("At least one share", ex.Message);
    }

    [Fact]
    public void Combine_UnsupportedVersion_ThrowsValidationException()
    {
        // Create a share with version=2 (not the current version)
        var share = new ShamirShare(2, 257, 2, 1, [100], 1);

        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine([share]));

        Assert.Contains("Unsupported share version", ex.Message);
    }

    [Fact]
    public void Combine_UnsupportedPrime_ThrowsValidationException()
    {
        // Create a share with non-257 prime
        var share = new ShamirShare(1, 263, 2, 1, [100], 1);

        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine([share]));

        Assert.Contains("Unsupported prime", ex.Message);
    }

    [Fact]
    public void Combine_SecretLengthMismatch_ThrowsValidationException()
    {
        // Two shares with different secret lengths
        var share1 = new ShamirShare(1, 257, 2, 1, [100, 200], 2);
        var share2 = new ShamirShare(1, 257, 2, 2, [150], 1);

        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine([share1, share2]));

        Assert.Contains("secret length", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Combine_ExactlyOneShareBelowThreshold_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("x"), shareCount: 5, threshold: 3);

        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine([shares[0]]));

        Assert.Contains("Insufficient shares", ex.Message);
    }

    [Fact]
    public void Combine_SharesFromDifferentSecrets_DoNotReconstructEither()
    {
        var secret1 = Encoding.UTF8.GetBytes("secret-one");
        var secret2 = Encoding.UTF8.GetBytes("secret-two");
        var shares1 = ShamirSecretSharer.Split(secret1, shareCount: 3, threshold: 2);
        var shares2 = ShamirSecretSharer.Split(secret2, shareCount: 3, threshold: 2);

        // Mix shares from different secrets (same metadata shape, so passes validation)
        var reconstructed = ShamirSecretSharer.Combine([shares1[0], shares2[1]]);

        // Should reconstruct something, but NOT either original secret
        Assert.NotEqual(secret1, reconstructed);
        Assert.NotEqual(secret2, reconstructed);
    }
}
