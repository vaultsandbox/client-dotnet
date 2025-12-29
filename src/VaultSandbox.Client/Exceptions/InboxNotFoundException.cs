namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when an inbox is not found (404).
/// </summary>
public class InboxNotFoundException : VaultSandboxException
{
    public string EmailAddress { get; }

    public InboxNotFoundException(string emailAddress)
        : base($"Inbox not found: {emailAddress}")
    {
        EmailAddress = emailAddress;
    }
}
