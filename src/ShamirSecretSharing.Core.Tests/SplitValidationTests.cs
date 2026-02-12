using System.Text;
using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class SplitValidationTests
{
    private static readonly byte[] ValidSecret = Encoding.UTF8.GetBytes("test");

    [Fact]
    public void Split_NullSecret_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShamirSecretSharer.Split(null!, shareCount: 5, threshold: 3));
    }

    [Fact]
    public void Split_EmptySecret_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ShamirSecretSharer.Split([], shareCount: 5, threshold: 3));
    }

    [Fact]
    public void Split_ShareCountLessThanTwo_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 1, threshold: 1));
        Assert.Contains("Share count", ex.Message);
    }

    [Fact]
    public void Split_ShareCountEqualsPrime_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 257, threshold: 3));
        Assert.Contains("Share count", ex.Message);
    }

    [Fact]
    public void Split_ShareCountExceedsPrime_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 300, threshold: 3));
        Assert.Contains("Share count", ex.Message);
    }

    [Fact]
    public void Split_ThresholdLessThanTwo_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 5, threshold: 1));
        Assert.Contains("Threshold", ex.Message);
    }

    [Fact]
    public void Split_ThresholdExceedsShareCount_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 5, threshold: 6));
        Assert.Contains("Threshold", ex.Message);
    }

    [Fact]
    public void Split_ThresholdZero_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: 5, threshold: 0));
    }

    [Fact]
    public void Split_NegativeShareCount_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharer.Split(ValidSecret, shareCount: -1, threshold: 2));
    }
}
