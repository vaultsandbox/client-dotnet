namespace VaultSandbox.Client.Api;

/// <summary>
/// Lightweight email metadata without body content.
/// </summary>
public sealed record EmailMetadata(
    string Id,
    string From,
    string Subject,
    DateTimeOffset ReceivedAt,
    bool IsRead);
