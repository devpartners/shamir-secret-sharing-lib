namespace ShamirSecretSharing.Core;

public class ShamirValidationException : Exception
{
    public ShamirValidationException(string message)
        : base(message)
    {
    }

    public ShamirValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
