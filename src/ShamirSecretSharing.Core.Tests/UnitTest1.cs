using System.Text;
using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class ShamirSecretSharerTests
{
    [Fact]
    public void SplitAndCombine_ReconstructsOriginalSecret()
    {
        var secret = Encoding.UTF8.GetBytes("Launch code: 12345");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[2], shares[4]]);

        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void SplitAndCombine_ReconstructsBinarySecret()
    {
        byte[] secret = [0x00, 0x01, 0x7F, 0x80, 0xFF];
        var shares = ShamirSecretSharer.Split(secret, shareCount: 6, threshold: 4);

        var reconstructed = ShamirSecretSharer.Combine([shares[0], shares[1], shares[3], shares[5]]);

        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public void Combine_WithAnyThresholdSubset_ReconstructsSecret()
    {
        var secret = Encoding.UTF8.GetBytes("subset-check");
        var shares = ShamirSecretSharer.Split(secret, shareCount: 5, threshold: 3);

        var subsets = new[]
        {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 4 },
            new[] { 1, 3, 4 }
        };

        foreach (var subset in subsets)
        {
            var selected = subset.Select(index => shares[index]).ToArray();
            var reconstructed = ShamirSecretSharer.Combine(selected);
            Assert.Equal(secret, reconstructed);
        }
    }

    [Fact]
    public void Combine_WithInsufficientShares_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("abc"), shareCount: 5, threshold: 4);

        var exception = Assert.Throws<ShamirValidationException>(() => ShamirSecretSharer.Combine([shares[0], shares[1], shares[2]]));

        Assert.Contains("Insufficient shares", exception.Message);
    }

    [Fact]
    public void Combine_WithDuplicateX_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("duplicate"), shareCount: 4, threshold: 3);
        var duplicateXShare = new ShamirShare(
            shares[1].Version,
            shares[1].Prime,
            shares[1].Threshold,
            shares[0].X,
            shares[1].YValues.ToArray(),
            shares[1].SecretLength);

        var exception = Assert.Throws<ShamirValidationException>(() => ShamirSecretSharer.Combine([shares[0], duplicateXShare, shares[2]]));

        Assert.Contains("Duplicate x value", exception.Message);
    }

    [Fact]
    public void Combine_WithThresholdMismatch_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("threshold"), shareCount: 4, threshold: 3);
        var mismatch = new ShamirShare(
            shares[1].Version,
            shares[1].Prime,
            shares[1].Threshold + 1,
            shares[1].X,
            shares[1].YValues.ToArray(),
            shares[1].SecretLength);

        var exception = Assert.Throws<ShamirValidationException>(() => ShamirSecretSharer.Combine([shares[0], mismatch, shares[2], shares[3]]));

        Assert.Contains("threshold", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Combine_WithPrimeMismatch_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("prime"), shareCount: 4, threshold: 3);
        var mismatch = new ShamirShare(
            shares[1].Version,
            263,
            shares[1].Threshold,
            shares[1].X,
            shares[1].YValues.ToArray(),
            shares[1].SecretLength);

        var exception = Assert.Throws<ShamirValidationException>(() => ShamirSecretSharer.Combine([shares[0], mismatch, shares[2], shares[3]]));

        Assert.Contains("prime", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Combine_WithVersionMismatch_ThrowsValidationException()
    {
        var shares = ShamirSecretSharer.Split(Encoding.UTF8.GetBytes("version"), shareCount: 4, threshold: 3);
        var mismatch = new ShamirShare(
            shares[1].Version + 1,
            shares[1].Prime,
            shares[1].Threshold,
            shares[1].X,
            shares[1].YValues.ToArray(),
            shares[1].SecretLength);

        var exception = Assert.Throws<ShamirValidationException>(() => ShamirSecretSharer.Combine([shares[0], mismatch, shares[2], shares[3]]));

        Assert.Contains("version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Split_WithInjectedRandom_ProducesDeterministicShares()
    {
        byte[] secret = [5];
        var random = new SequenceFieldRandom(1, 2);

        var shares = ShamirSecretSharer.Split(secret, shareCount: 3, threshold: 3, random);

        Assert.Equal((ushort)8, shares[0].YValues[0]);
        Assert.Equal((ushort)15, shares[1].YValues[0]);
        Assert.Equal((ushort)26, shares[2].YValues[0]);
    }

    [Fact]
    public void ShareJsonCodec_RoundTripsShare()
    {
        var share = new ShamirShare(1, 257, 3, 5, [10, 20, 30], 3);

        var json = ShareJsonCodec.Serialize(share);
        var reconstructed = ShareJsonCodec.Deserialize(json);

        Assert.Equal(share.Version, reconstructed.Version);
        Assert.Equal(share.Prime, reconstructed.Prime);
        Assert.Equal(share.Threshold, reconstructed.Threshold);
        Assert.Equal(share.X, reconstructed.X);
        Assert.Equal(share.SecretLength, reconstructed.SecretLength);
        Assert.Equal(share.YValues, reconstructed.YValues);
    }

    [Fact]
    public void ShareJsonCodec_DeserializeMany_RejectsEmptyInput()
    {
        var exception = Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.DeserializeMany(["", "   "]));

        Assert.Contains("No share JSON entries", exception.Message);
    }

    [Fact]
    public void ShareJsonCodec_Deserialize_RejectsMalformedJson()
    {
        Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize("{not-json}"));
    }

    [Fact]
    public void Combine_WhenReconstructedValueExceedsByteRange_ThrowsReconstructionException()
    {
        var share1 = new ShamirShare(1, 257, 2, 1, [0], 1);
        var share2 = new ShamirShare(1, 257, 2, 2, [1], 1);

        var exception = Assert.Throws<ShamirReconstructionException>(() => ShamirSecretSharer.Combine([share1, share2]));

        Assert.Contains("outside byte range", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShamirShare_WithOutOfRangeYValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShamirShare(1, 257, 2, 1, [300], 1));
    }

    private sealed class SequenceFieldRandom : IFieldRandom
    {
        private readonly Queue<int> _values;

        public SequenceFieldRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int NextInt(int maxExclusive)
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("No deterministic random values remain.");
            }

            var next = _values.Dequeue();
            if (next < 0 || next >= maxExclusive)
            {
                throw new InvalidOperationException($"Deterministic value {next} is outside requested range [0, {maxExclusive}).");
            }

            return next;
        }
    }
}
