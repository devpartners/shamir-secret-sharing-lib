using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class ShamirShareConstructorTests
{
    [Fact]
    public void Constructor_VersionZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(0, 257, 2, 1, [100], 1));
    }

    [Fact]
    public void Constructor_NegativeVersion_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(-1, 257, 2, 1, [100], 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    public void Constructor_PrimeTooSmall_Throws(int prime)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, prime, 2, 1, [0], 1));
    }

    [Fact]
    public void Constructor_ThresholdLessThanTwo_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 1, 1, [100], 1));
    }

    [Fact]
    public void Constructor_ThresholdEqualsPrime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 257, 1, [100], 1));
    }

    [Fact]
    public void Constructor_ThresholdExceedsPrime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 258, 1, [100], 1));
    }

    [Fact]
    public void Constructor_XZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 0, [100], 1));
    }

    [Fact]
    public void Constructor_XNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, -1, [100], 1));
    }

    [Fact]
    public void Constructor_XEqualsPrime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 257, [100], 1));
    }

    [Fact]
    public void Constructor_SecretLengthZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 1, [], 0));
    }

    [Fact]
    public void Constructor_SecretLengthNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 1, [], -1));
    }

    [Fact]
    public void Constructor_NullYValues_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ShamirShare(1, 257, 2, 1, null!, 1));
    }

    [Fact]
    public void Constructor_YValuesLengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ShamirShare(1, 257, 2, 1, [100, 200], 1));
    }

    [Fact]
    public void Constructor_YValueEqualsPrime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 1, [257], 1));
    }

    [Fact]
    public void Constructor_YValueExceedsPrime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ShamirShare(1, 257, 2, 1, [300], 1));
    }

    [Fact]
    public void Constructor_ValidParameters_SetsAllProperties()
    {
        var share = new ShamirShare(1, 257, 3, 5, [10, 20, 30], 3);

        Assert.Equal(1, share.Version);
        Assert.Equal(257, share.Prime);
        Assert.Equal(3, share.Threshold);
        Assert.Equal(5, share.X);
        Assert.Equal(3, share.SecretLength);
        Assert.Equal(new ushort[] { 10, 20, 30 }, share.YValues);
    }

    [Fact]
    public void Constructor_DefensiveCopy_MutationDoesNotAffectShare()
    {
        ushort[] original = [10, 20, 30];
        var share = new ShamirShare(1, 257, 3, 5, original, 3);

        original[0] = 999;

        Assert.Equal(10, share.YValues[0]);
    }

    [Fact]
    public void SimplifiedConstructor_UsesCurrentVersionAndPrime()
    {
        var share = new ShamirShare(3, 1, [100], 1);

        Assert.Equal(ShamirShare.CurrentVersion, share.Version);
        Assert.Equal(ShamirShare.CurrentPrime, share.Prime);
    }

    [Fact]
    public void Constructor_MaxValidX_Succeeds()
    {
        // x = prime - 1 = 256 should be valid
        var share = new ShamirShare(1, 257, 2, 256, [100], 1);
        Assert.Equal(256, share.X);
    }

    [Fact]
    public void Constructor_MaxValidThreshold_Succeeds()
    {
        // threshold = prime - 1 = 256 should be valid
        var share = new ShamirShare(1, 257, 256, 1, [100], 1);
        Assert.Equal(256, share.Threshold);
    }
}
