namespace ShamirSecretSharing.Core;

public class ShamirReconstructionException : Exception
{
    public ShamirReconstructionException(string message)
        : base(message)
    {
    }

    public ShamirReconstructionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
