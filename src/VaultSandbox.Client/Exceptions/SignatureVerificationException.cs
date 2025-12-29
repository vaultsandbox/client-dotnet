namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when signature verification fails.
/// CRITICAL: This indicates potential tampering. Never ignore this exception.
/// </summary>
public class SignatureVerificationException : VaultSandboxException
{
    public SignatureVerificationException(string message) : base(message) { }

    public SignatureVerificationException(string message, Exception innerException)
        : base(message, innerException) { }
}
