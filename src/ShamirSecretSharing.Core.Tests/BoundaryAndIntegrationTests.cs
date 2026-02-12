using System.Text;
using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class BoundaryAndIntegrationTests
{
    // ── Minimum boundary conditions ───────────────────────────

    [Fact]
    public void SplitAndCombine_MinimumShareCount_TwoOfTwo()
    {
        byte[] secret = [42];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 2, threshold: 2);

        Assert.Equal(2, shares.Count);

        var reconstructed = ShamirSecretSharer.Combine(shares);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_ThresholdEqualsShareCount()
    {
        var secret = Encoding.UTF8.GetBytes("all-required");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 5);

        var reconstructed = ShamirSecretSharer.Combine(shares);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_ThresholdEqualsShareCount_InsufficientFails()
    {
        var secret = Encoding.UTF8.GetBytes("all-required");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 5);

        Assert.Throws<ShamirValidationException>(() =>
            ShamirSecretSharer.Combine(shares.Take(4).ToArray()));
    }

    [Fact]
    public void SplitAndCombine_SingleByteSecret()
    {
        byte[] secret = [0xAB];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 3, threshold: 2);

        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[2]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_MaxShareCount256()
    {
        byte[] secret = [99];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 256, threshold: 2);

        Assert.Equal(256, shares.Count);

        // Reconstruct using first and last shares
        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[255]]);
        Assert.Equal(secret, reconstructed);
    }

    // ── Special secret values ─────────────────────────────────

    [Fact]
    public void SplitAndCombine_AllZerosSecret()
    {
        byte[] secret = [0x00, 0x00, 0x00, 0x00];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 4, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[1], shares[2]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_AllMaxByteSecret()
    {
        byte[] secret = [0xFF, 0xFF, 0xFF, 0xFF];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 4, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[1], shares[2], shares[3]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_AllByteValues()
    {
        // Secret contains every possible byte value
        var secret = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            secret[i] = (byte)i;
        }

        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);
        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[2], shares[4]]);
        Assert.Equal(secret, reconstructed);
    }

    // ── Share selection patterns ──────────────────────────────

    [Fact]
    public void Combine_FirstNShares_Reconstructs()
    {
        var secret = Encoding.UTF8.GetBytes("order-test");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[1], shares[2]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void Combine_LastNShares_Reconstructs()
    {
        var secret = Encoding.UTF8.GetBytes("order-test");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[2], shares[3], shares[4]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void Combine_ReverseOrder_Reconstructs()
    {
        var secret = Encoding.UTF8.GetBytes("reverse-order");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[4], shares[2], shares[0]]);
        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void Combine_MoreSharesThanThreshold_Reconstructs()
    {
        var secret = Encoding.UTF8.GetBytes("extra-shares");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        // Provide all 5 shares when only 3 are needed
        var reconstructed = ShamirSecretSharer.Combine(shares);
        Assert.Equal(secret, reconstructed);
    }

    // ── End-to-end roundtrip through JSON ─────────────────────

    [Fact]
    public void EndToEnd_SplitSerializeDeserializeCombine()
    {
        var secret = Encoding.UTF8.GetBytes("full-roundtrip-test!");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        // Serialize all shares to JSON
        var jsonLines = shares.Select(ShareJsonCodec.Serialize).ToList();

        // Deserialize back
        var deserializedShares = ShareJsonCodec.DeserializeMany(jsonLines);

        // Combine using a subset
        var reconstructed = ShamirSecretSharer.Combine(
            deserializedShares.Take(3).ToArray());

        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void EndToEnd_IndividualJsonRoundTrip()
    {
        var secret = Encoding.UTF8.GetBytes("per-share-json");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 4, threshold: 3);

        // Serialize and deserialize each share individually
        var restored = shares
            .Select(s => ShareJsonCodec.Deserialize(ShareJsonCodec.Serialize(s)))
            .ToList();

        var reconstructed = ShamirSecretSharer.Combine([restored[0], restored[1], restored[3]]);
        Assert.Equal(secret, reconstructed);
    }

    // ── Larger secrets ────────────────────────────────────────

    [Fact]
    public void SplitAndCombine_LargerSecret_256Bytes()
    {
        var secret = new byte[256];
        new Random(42).NextBytes(secret);

        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);
        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[2], shares[4]]);
        Assert.Equal(secret, reconstructed);
    }

    // ── Share metadata correctness ────────────────────────────

    [Fact]
    public void Split_ProducesSharesWithCorrectMetadata()
    {
        byte[] secret = [10, 20, 30];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 4, threshold: 3);

        Assert.Equal(4, shares.Count);
        foreach (var share in shares)
        {
            Assert.Equal(ShamirShare.CurrentVersion, share.Version);
            Assert.Equal(ShamirShare.CurrentPrime, share.Prime);
            Assert.Equal(3, share.Threshold);
            Assert.Equal(3, share.SecretLength);
            Assert.Equal(3, share.YValues.Length);
        }

        // X values should be 1..4
        Assert.Equal(1, shares[0].X);
        Assert.Equal(2, shares[1].X);
        Assert.Equal(3, shares[2].X);
        Assert.Equal(4, shares[3].X);
    }

    // ── Each share's Y values are within GF(257) ──────────────

    [Fact]
    public void Split_AllYValuesWithinField()
    {
        var secret = new byte[64];
        new Random(99).NextBytes(secret);

        var shares = ShamirSecretSharer.Split(secret, shareCount: 10, threshold: 5);

        foreach (var share in shares)
        {
            foreach (var y in share.YValues)
            {
                Assert.True(y < 257, $"Y value {y} is outside GF(257)");
            }
        }
    }

    // ── Minimum threshold=2 with various share counts ─────────

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(50)]
    public void SplitAndCombine_ThresholdTwo_VariousShareCounts(int shareCount)
    {
        var secret = Encoding.UTF8.GetBytes("threshold-two");
        var shares = ShamirSecretSharer.Split(secret, shareCount: shareCount, threshold: 2);

        Assert.Equal(shareCount, shares.Count);

        // Any two shares should reconstruct
        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[shareCount - 1]]);
        Assert.Equal(secret, reconstructed);
    }
}
