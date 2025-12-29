namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception thrown when the server signing key in a payload doesn't match the expected key.
/// CRITICAL: This indicates a potential key substitution attack. Never ignore this exception.
/// </summary>
public class ServerKeyMismatchException : VaultSandboxException
{
    /// <summary>
    /// The expected server signing key (base64url-encoded).
    /// </summary>
    public string ExpectedKey { get; }

    /// <summary>
    /// The actual server signing key received in the payload (base64url-encoded).
    /// </summary>
    public string ActualKey { get; }

    public ServerKeyMismatchException(string expectedKey, string actualKey)
        : base($"Server signing key mismatch. Expected key does not match the key in the payload. This may indicate a key substitution attack.")
    {
        ExpectedKey = expectedKey;
        ActualKey = actualKey;
    }

    public ServerKeyMismatchException(string expectedKey, string actualKey, string message)
        : base(message)
    {
        ExpectedKey = expectedKey;
        ActualKey = actualKey;
    }
}
