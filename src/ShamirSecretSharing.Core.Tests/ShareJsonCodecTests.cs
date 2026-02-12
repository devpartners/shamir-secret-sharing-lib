using ShamirSecretSharing.Core;

namespace ShamirSecretSharing.Core.Tests;

public class ShareJsonCodecTests
{
    [Fact]
    public void Serialize_NullShare_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShareJsonCodec.Serialize(null!));
    }

    [Fact]
    public void Deserialize_NullInput_ThrowsValidationException()
    {
        var ex = Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize(null!));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_EmptyString_ThrowsValidationException()
    {
        var ex = Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize(""));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WhitespaceOnly_ThrowsValidationException()
    {
        var ex = Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize("   "));
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_InvalidFieldValues_ThrowsValidationException()
    {
        // Valid JSON structure but with invalid field values (version=0 triggers constructor exception)
        var json = """{"version":0,"prime":257,"threshold":2,"x":1,"yValues":[100],"secretLength":1}""";

        Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize(json));
    }

    [Fact]
    public void Deserialize_MalformedJson_ThrowsValidationException()
    {
        Assert.Throws<ShamirValidationException>(() => ShareJsonCodec.Deserialize("{not-json}"));
    }

    [Fact]
    public void Deserialize_CaseInsensitive_Works()
    {
        // Use PascalCase property names instead of camelCase
        var json = """{"Version":1,"Prime":257,"Threshold":2,"X":1,"YValues":[100],"SecretLength":1}""";

        var share = ShareJsonCodec.Deserialize(json);

        Assert.Equal(1, share.Version);
        Assert.Equal(100, share.YValues[0]);
    }

    [Fact]
    public void DeserializeMany_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ShareJsonCodec.DeserializeMany(null!));
    }

    [Fact]
    public void DeserializeMany_AllEmpty_ThrowsValidationException()
    {
        var ex = Assert.Throws<ShamirValidationException>(() =>
            ShareJsonCodec.DeserializeMany(["", "  ", "\t"]));

        Assert.Contains("No share JSON entries", ex.Message);
    }

    [Fact]
    public void DeserializeMany_ValidLines_ReturnsAllShares()
    {
        var share1 = new ShamirShare(1, 257, 2, 1, [100], 1);
        var share2 = new ShamirShare(1, 257, 2, 2, [200], 1);
        var json1 = ShareJsonCodec.Serialize(share1);
        var json2 = ShareJsonCodec.Serialize(share2);

        var result = ShareJsonCodec.DeserializeMany([json1, json2]);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].X);
        Assert.Equal(2, result[1].X);
    }

    [Fact]
    public void DeserializeMany_MixedValidAndEmpty_SkipsEmptyLines()
    {
        var share = new ShamirShare(1, 257, 2, 1, [100], 1);
        var json = ShareJsonCodec.Serialize(share);

        var result = ShareJsonCodec.DeserializeMany(["", json, "  ", ""]);

        Assert.Single(result);
        Assert.Equal(1, result[0].X);
    }

    [Fact]
    public void DeserializeMany_InvalidJsonAmongValid_ThrowsValidationException()
    {
        var share = new ShamirShare(1, 257, 2, 1, [100], 1);
        var json = ShareJsonCodec.Serialize(share);

        Assert.Throws<ShamirValidationException>(() =>
            ShareJsonCodec.DeserializeMany([json, "{bad-json}", json]));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesAllFields()
    {
        var original = new ShamirShare(1, 257, 4, 42, [0, 128, 256, 1, 100], 5);

        var json = ShareJsonCodec.Serialize(original);
        var restored = ShareJsonCodec.Deserialize(json);

        Assert.Equal(original.Version, restored.Version);
        Assert.Equal(original.Prime, restored.Prime);
        Assert.Equal(original.Threshold, restored.Threshold);
        Assert.Equal(original.X, restored.X);
        Assert.Equal(original.SecretLength, restored.SecretLength);
        Assert.Equal(original.YValues, restored.YValues);
    }
}
