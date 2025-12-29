namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception for operation timeouts.
/// </summary>
public class VaultSandboxTimeoutException : VaultSandboxException
{
    public TimeSpan Timeout { get; }

    public VaultSandboxTimeoutException(TimeSpan timeout)
        : base($"Operation timed out after {timeout.TotalMilliseconds}ms")
    {
        Timeout = timeout;
    }

    public VaultSandboxTimeoutException(string message, TimeSpan timeout)
        : base(message)
    {
        Timeout = timeout;
    }
}
