namespace ShamirSecretSharing.Core;

public static class ShamirSecretSharer
{
    public static IReadOnlyList<ShamirShare> Split(byte[] secret, int shareCount, int threshold, IFieldRandom? random = null)
    {
        if (secret is null)
        {
            throw new ArgumentNullException(nameof(secret));
        }

        if (secret.Length == 0)
        {
            throw new ArgumentException("Secret must not be empty.", nameof(secret));
        }

        if (shareCount < 2 || shareCount >= Gf257.Prime)
        {
            throw new ArgumentOutOfRangeException(nameof(shareCount), $"Share count must be between 2 and {Gf257.Prime - 1}.");
        }

        if (threshold < 2 || threshold > shareCount)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 2 and shareCount.");
        }

        random ??= new CryptoFieldRandom();

        var shareValues = new ushort[shareCount][];
        for (var i = 0; i < shareCount; i++)
        {
            shareValues[i] = new ushort[secret.Length];
        }

        var coefficients = threshold <= 64 ? stackalloc int[threshold] : new int[threshold];
        for (var secretIndex = 0; secretIndex < secret.Length; secretIndex++)
        {
            coefficients.Clear();
            coefficients[0] = secret[secretIndex];

            for (var degree = 1; degree < threshold; degree++)
            {
                var coefficient = random.NextInt(Gf257.Prime);
                if (coefficient < 0 || coefficient >= Gf257.Prime)
                {
                    throw new ShamirValidationException($"Random coefficient {coefficient} is outside GF(257).");
                }

                coefficients[degree] = coefficient;
            }

            for (var shareIndex = 0; shareIndex < shareCount; shareIndex++)
            {
                var x = shareIndex + 1;
                var y = Gf257.EvaluatePolynomial(coefficients, x);
                shareValues[shareIndex][secretIndex] = (ushort)y;
            }
        }

        var shares = new List<ShamirShare>(shareCount);
        for (var shareIndex = 0; shareIndex < shareCount; shareIndex++)
        {
            shares.Add(
                new ShamirShare(
                    ShamirShare.CurrentVersion,
                    ShamirShare.CurrentPrime,
                    threshold,
                    shareIndex + 1,
                    shareValues[shareIndex],
                    secret.Length));
        }

        return shares;
    }

    public static byte[] Combine(IEnumerable<ShamirShare> shares)
    {
        if (shares is null)
        {
            throw new ArgumentNullException(nameof(shares));
        }

        var shareList = new List<ShamirShare>();
        foreach (var share in shares)
        {
            if (share is null)
            {
                throw new ShamirValidationException("Share collection contains a null share.");
            }

            shareList.Add(share);
        }

        if (shareList.Count == 0)
        {
            throw new ShamirValidationException("At least one share is required.");
        }

        var firstShare = shareList[0];
        if (firstShare.Version != ShamirShare.CurrentVersion)
        {
            throw new ShamirValidationException($"Unsupported share version {firstShare.Version}. Expected {ShamirShare.CurrentVersion}.");
        }

        if (firstShare.Prime != Gf257.Prime)
        {
            throw new ShamirValidationException($"Unsupported prime {firstShare.Prime}. Expected {Gf257.Prime}.");
        }

        ValidateShareCollection(shareList, firstShare);

        if (shareList.Count < firstShare.Threshold)
        {
            throw new ShamirValidationException(
                $"Insufficient shares. Threshold is {firstShare.Threshold}, but only {shareList.Count} share(s) were provided.");
        }

        var selectedShares = shareList
            .OrderBy(share => share.X)
            .Take(firstShare.Threshold)
            .ToArray();

        var secret = new byte[firstShare.SecretLength];
        var threshold = firstShare.Threshold;
        var xValues = threshold <= 256 ? stackalloc int[threshold] : new int[threshold];
        var yValues = threshold <= 256 ? stackalloc int[threshold] : new int[threshold];

        for (var i = 0; i < threshold; i++)
        {
            xValues[i] = selectedShares[i].X;
        }

        for (var secretIndex = 0; secretIndex < secret.Length; secretIndex++)
        {
            for (var i = 0; i < threshold; i++)
            {
                yValues[i] = selectedShares[i].YValues[secretIndex];
            }

            var value = Gf257.InterpolateAtZero(xValues, yValues);
            if (value < 0 || value > byte.MaxValue)
            {
                throw new ShamirReconstructionException(
                    $"Reconstructed value {value} at byte index {secretIndex} is outside byte range.");
            }

            secret[secretIndex] = (byte)value;
        }

        return secret;
    }

    private static void ValidateShareCollection(IReadOnlyList<ShamirShare> shares, ShamirShare firstShare)
    {
        var distinctX = new HashSet<int>();

        for (var shareIndex = 0; shareIndex < shares.Count; shareIndex++)
        {
            var share = shares[shareIndex];

            if (!distinctX.Add(share.X))
            {
                throw new ShamirValidationException($"Duplicate x value detected: {share.X}.");
            }

            if (share.Version != firstShare.Version)
            {
                throw new ShamirValidationException(
                    $"Share metadata mismatch at index {shareIndex}: version {share.Version} does not match {firstShare.Version}.");
            }

            if (share.Prime != firstShare.Prime)
            {
                throw new ShamirValidationException(
                    $"Share metadata mismatch at index {shareIndex}: prime {share.Prime} does not match {firstShare.Prime}.");
            }

            if (share.Threshold != firstShare.Threshold)
            {
                throw new ShamirValidationException(
                    $"Share metadata mismatch at index {shareIndex}: threshold {share.Threshold} does not match {firstShare.Threshold}.");
            }

            if (share.SecretLength != firstShare.SecretLength)
            {
                throw new ShamirValidationException(
                    $"Share metadata mismatch at index {shareIndex}: secret length {share.SecretLength} does not match {firstShare.SecretLength}.");
            }

            if (share.YValues.Length != firstShare.SecretLength)
            {
                throw new ShamirValidationException(
                    $"Share at index {shareIndex} has {share.YValues.Length} y-values, expected {firstShare.SecretLength}.");
            }

            if (share.X <= 0 || share.X >= firstShare.Prime)
            {
                throw new ShamirValidationException($"Share at index {shareIndex} has x={share.X}, which is outside the field.");
            }

            for (var i = 0; i < share.YValues.Length; i++)
            {
                if (share.YValues[i] >= firstShare.Prime)
                {
                    throw new ShamirValidationException(
                        $"Share at index {shareIndex} has yValues[{i}]={share.YValues[i]}, which is outside the field.");
                }
            }
        }
    }
}
