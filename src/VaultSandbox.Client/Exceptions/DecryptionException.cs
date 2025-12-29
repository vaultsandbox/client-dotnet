namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when decryption fails.
/// CRITICAL: This exception should never be silently ignored.
/// </summary>
public class DecryptionException : VaultSandboxException
{
    public DecryptionException(string message) : base(message) { }

    public DecryptionException(string message, Exception innerException)
        : base(message, innerException) { }
}
