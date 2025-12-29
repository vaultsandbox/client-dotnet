namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Base exception for all VaultSandbox client errors.
/// </summary>
public class VaultSandboxException : Exception
{
    public VaultSandboxException() { }

    public VaultSandboxException(string message) : base(message) { }

    public VaultSandboxException(string message, Exception innerException)
        : base(message, innerException) { }
}
