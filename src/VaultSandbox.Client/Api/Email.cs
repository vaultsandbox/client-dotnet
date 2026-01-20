namespace VaultSandbox.Client.Api;

/// <summary>
/// Represents a decrypted email with full content and metadata.
/// </summary>
public sealed class Email
{
    private readonly IInbox? _inbox;

    /// <summary>
    /// Unique identifier for this email.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The inbox hash this email belongs to.
    /// </summary>
    public required string InboxId { get; init; }

    /// <summary>
    /// Sender email address.
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// List of recipient email addresses.
    /// </summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// When the email was received by the server.
    /// </summary>
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>
    /// Whether the email has been marked as read.
    /// </summary>
    public bool IsRead { get; internal set; }

    /// <summary>
    /// Plain text content of the email.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// HTML content of the email.
    /// </summary>
    public string? Html { get; init; }

    /// <summary>
    /// Email headers - values can be strings or complex objects.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Headers { get; init; }

    /// <summary>
    /// List of email attachments.
    /// </summary>
    public IReadOnlyList<EmailAttachment>? Attachments { get; init; }

    /// <summary>
    /// Links extracted from the email body.
    /// </summary>
    public IReadOnlyList<string>? Links { get; init; }

    /// <summary>
    /// Email authentication results (SPF, DKIM, DMARC).
    /// </summary>
    public AuthenticationResults? AuthResults { get; init; }

    /// <summary>
    /// Additional metadata associated with the email.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Spam analysis results from Rspamd.
    /// Null if spam analysis is not enabled on the server.
    /// </summary>
    public SpamAnalysisResult? SpamAnalysis { get; init; }

    /// <summary>
    /// Returns whether this email is classified as spam.
    /// Returns null if spam analysis was not performed or status is not 'analyzed'.
    /// </summary>
    public bool? GetIsSpam()
    {
        if (SpamAnalysis is null || SpamAnalysis.Status != SpamAnalysisStatus.Analyzed)
        {
            return null;
        }
        return SpamAnalysis.IsSpam;
    }

    /// <summary>
    /// Returns the spam score for this email.
    /// Returns null if spam analysis was not performed or status is not 'analyzed'.
    /// </summary>
    public double? GetSpamScore()
    {
        if (SpamAnalysis is null || SpamAnalysis.Status != SpamAnalysisStatus.Analyzed)
        {
            return null;
        }
        return SpamAnalysis.Score;
    }

    /// <summary>
    /// Creates a new email instance.
    /// </summary>
    public Email()
    {
    }

    /// <summary>
    /// Internal constructor used by Inbox when creating emails.
    /// </summary>
    internal Email(IInbox inbox)
    {
        _inbox = inbox;
    }

    /// <summary>
    /// Marks this email as read.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the email was not retrieved through an inbox.
    /// </exception>
    public async Task MarkAsReadAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        await _inbox!.MarkAsReadAsync(Id, ct);
        IsRead = true;
    }

    /// <summary>
    /// Deletes this email from the inbox.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the email was not retrieved through an inbox.
    /// </exception>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        await _inbox!.DeleteEmailAsync(Id, ct);
    }

    /// <summary>
    /// Gets the raw source of this email.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw email content as a string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the email was not retrieved through an inbox.
    /// </exception>
    public async Task<string> GetRawAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        return await _inbox!.GetEmailRawAsync(Id, ct);
    }

    private void EnsureInboxAvailable()
    {
        if (_inbox is null)
        {
            throw new InvalidOperationException(
                "This operation requires the email to be retrieved through an inbox. " +
                "Emails created manually or deserialized cannot use instance methods.");
        }
    }
}
