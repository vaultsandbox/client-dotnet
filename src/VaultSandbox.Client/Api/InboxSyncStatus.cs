namespace VaultSandbox.Client.Api;

/// <summary>
/// Synchronization status of an inbox for efficient change detection.
/// </summary>
public sealed record InboxSyncStatus
{
    /// <summary>
    /// Number of emails currently in the inbox.
    /// </summary>
    public required int EmailCount { get; init; }

    /// <summary>
    /// Hash of the email list. Changes when emails are added, removed, or modified.
    /// Use this to detect changes without fetching all emails.
    /// </summary>
    public required string EmailsHash { get; init; }
}
