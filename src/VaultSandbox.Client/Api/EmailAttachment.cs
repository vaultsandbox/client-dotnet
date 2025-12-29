namespace VaultSandbox.Client.Api;

/// <summary>
/// Represents an email attachment.
/// </summary>
public sealed record EmailAttachment
{
    /// <summary>
    /// The filename of the attachment.
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// MIME content type (e.g., "application/pdf").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Content-ID for inline attachments (e.g., "cid:image001").
    /// </summary>
    public string? ContentId { get; init; }

    /// <summary>
    /// Content disposition ("attachment" or "inline").
    /// </summary>
    public string? ContentDisposition { get; init; }

    /// <summary>
    /// The decoded binary content of the attachment.
    /// </summary>
    public required byte[] Content { get; init; }

    /// <summary>
    /// Optional SHA-256 checksum for integrity verification.
    /// </summary>
    public string? Checksum { get; init; }
}
