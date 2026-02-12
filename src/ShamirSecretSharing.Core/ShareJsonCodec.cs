using System.Text.Json;

namespace ShamirSecretSharing.Core;

public static class ShareJsonCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(ShamirShare share)
    {
        if (share is null)
        {
            throw new ArgumentNullException(nameof(share));
        }

        return JsonSerializer.Serialize(share, JsonOptions);
    }

    public static ShamirShare Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ShamirValidationException("Share JSON input must not be empty.");
        }

        try
        {
            var share = JsonSerializer.Deserialize<ShamirShare>(json, JsonOptions);
            if (share is null)
            {
                throw new ShamirValidationException("Share JSON did not produce a share value.");
            }

            return share;
        }
        catch (JsonException ex)
        {
            throw new ShamirValidationException("Share JSON could not be parsed.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new ShamirValidationException("Share JSON could not be mapped to a valid share model.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ShamirValidationException("Share JSON contains invalid share values.", ex);
        }
    }

    public static IReadOnlyList<ShamirShare> DeserializeMany(IEnumerable<string> jsonLines)
    {
        if (jsonLines is null)
        {
            throw new ArgumentNullException(nameof(jsonLines));
        }

        var shares = new List<ShamirShare>();

        foreach (var line in jsonLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            shares.Add(Deserialize(line));
        }

        if (shares.Count == 0)
        {
            throw new ShamirValidationException("No share JSON entries were provided.");
        }

        return shares;
    }
}
