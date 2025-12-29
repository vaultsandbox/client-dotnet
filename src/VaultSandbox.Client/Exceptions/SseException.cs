namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception for SSE connection issues.
/// </summary>
public class SseException : VaultSandboxException
{
    public SseException(string message) : base(message) { }

    public SseException(string message, Exception innerException)
        : base(message, innerException) { }
}
