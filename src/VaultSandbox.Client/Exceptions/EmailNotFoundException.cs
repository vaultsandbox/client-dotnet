namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when an email is not found (404).
/// </summary>
public class EmailNotFoundException : VaultSandboxException
{
    public string EmailId { get; }

    public EmailNotFoundException(string emailId)
        : base($"Email not found: {emailId}")
    {
        EmailId = emailId;
    }
}
