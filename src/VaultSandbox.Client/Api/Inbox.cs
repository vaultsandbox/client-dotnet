using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Implementation of an email inbox.
/// </summary>
internal sealed class Inbox : IInbox
{
    private readonly IVaultSandboxApiClient _apiClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IDeliveryStrategy _deliveryStrategy;
    private readonly MlKemKeyPair _keyPair;
    private readonly string _serverSigPk;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILogger<Inbox>? _logger;

    private readonly Channel<Email> _emailChannel;
    private bool _isSubscribed;
    private readonly SemaphoreSlim _subscriptionLock = new(1, 1);

    public string EmailAddress { get; }
    public DateTimeOffset ExpiresAt { get; }
    public string InboxHash { get; }
    public bool IsDisposed { get; private set; }

    internal Inbox(
        string emailAddress,
        DateTimeOffset expiresAt,
        string inboxHash,
        string serverSigPk,
        MlKemKeyPair keyPair,
        IVaultSandboxApiClient apiClient,
        ICryptoProvider cryptoProvider,
        IDeliveryStrategy deliveryStrategy,
        VaultSandboxClientOptions options,
        ILogger<Inbox>? logger = null)
    {
        EmailAddress = emailAddress;
        ExpiresAt = expiresAt;
        InboxHash = inboxHash;
        _serverSigPk = serverSigPk;
        _keyPair = keyPair;
        _apiClient = apiClient;
        _cryptoProvider = cryptoProvider;
        _deliveryStrategy = deliveryStrategy;
        _options = options;
        _logger = logger;

        _emailChannel = Channel.CreateUnbounded<Email>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });
    }

    public async Task<IReadOnlyList<Email>> GetEmailsAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // Ensure subscribed first - server requires subscription before inbox is active
        await EnsureSubscribedWithDefaultsAsync(ct);

        var encryptedEmails = await _apiClient.GetEmailsAsync(EmailAddress, ct);
        var emails = new List<Email>(encryptedEmails.Length);

        foreach (var encrypted in encryptedEmails)
        {
            var email = await DecryptEmailAsync(encrypted, ct);
            emails.Add(email);
        }

        return emails;
    }

    public async Task<Email> GetEmailAsync(string emailId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var encrypted = await _apiClient.GetEmailAsync(EmailAddress, emailId, ct);
        return await DecryptEmailAsync(encrypted, ct);
    }

    public async Task<string> GetEmailRawAsync(string emailId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var response = await _apiClient.GetRawEmailAsync(EmailAddress, emailId, ct);

        var rawBytes = await _cryptoProvider.DecryptAsync(
            response.EncryptedRaw,
            _keyPair.SecretKey,
            _serverSigPk,
            ct);

        // Decrypted content is base64-encoded - decode to get actual raw email
        var base64String = System.Text.Encoding.UTF8.GetString(rawBytes);
        var decodedBytes = Convert.FromBase64String(base64String);
        return System.Text.Encoding.UTF8.GetString(decodedBytes);
    }

    public async Task<Email> WaitForEmailAsync(
        WaitForEmailOptions? options = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        options ??= new WaitForEmailOptions();
        var timeout = options.Timeout ?? TimeSpan.FromMilliseconds(_options.WaitTimeoutMs);
        var pollInterval = options.PollInterval ?? TimeSpan.FromMilliseconds(_options.PollIntervalMs);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        // Subscribe first - this registers the inbox with the server for email reception
        await EnsureSubscribedAsync(pollInterval, linkedCts.Token);

        // Then check existing emails
        var existingEmails = await GetEmailsAsync(linkedCts.Token);
        var match = existingEmails.FirstOrDefault(e => options.Matches(e));
        if (match is not null)
        {
            return match;
        }

        try
        {
            await foreach (var email in _emailChannel.Reader.ReadAllAsync(linkedCts.Token))
            {
                if (options.Matches(email))
                {
                    return email;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new VaultSandboxTimeoutException(timeout);
        }

        throw new VaultSandboxTimeoutException(timeout);
    }

    public async IAsyncEnumerable<Email> WatchAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var defaultPollInterval = TimeSpan.FromMilliseconds(_options.PollIntervalMs);
        await EnsureSubscribedAsync(defaultPollInterval, ct);

        await foreach (var email in _emailChannel.Reader.ReadAllAsync(ct))
        {
            yield return email;
        }
    }

    public async Task MarkAsReadAsync(string emailId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _apiClient.MarkEmailAsReadAsync(EmailAddress, emailId, ct);
    }

    public async Task DeleteEmailAsync(string emailId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _apiClient.DeleteEmailAsync(EmailAddress, emailId, ct);
    }

    public Task<InboxExport> ExportAsync()
    {
        ThrowIfDisposed();

        var export = new InboxExport
        {
            EmailAddress = EmailAddress,
            ExpiresAt = ExpiresAt,
            InboxHash = InboxHash,
            ServerSigPk = _serverSigPk,
            PublicKeyB64 = Base64Url.Encode(_keyPair.PublicKey),
            SecretKeyB64 = Base64Url.Encode(_keyPair.SecretKey),
            ExportedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(export);
    }

    public async Task<int> GetEmailCountAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // Ensure subscribed first - server requires subscription before inbox is active
        await EnsureSubscribedWithDefaultsAsync(ct);

        var sync = await _apiClient.GetInboxSyncAsync(EmailAddress, ct);
        return sync.EmailCount;
    }

    public async Task<InboxSyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        // Ensure subscribed first - server requires subscription before inbox is active
        await EnsureSubscribedWithDefaultsAsync(ct);

        var sync = await _apiClient.GetInboxSyncAsync(EmailAddress, ct);

        _logger?.LogDebug(
            "Sync status for inbox {Email}: {Count} emails, hash: {Hash}",
            EmailAddress, sync.EmailCount, sync.EmailsHash);

        return new InboxSyncStatus
        {
            EmailCount = sync.EmailCount,
            EmailsHash = sync.EmailsHash
        };
    }

    public async Task WaitForEmailCountAsync(
        int count,
        WaitForEmailCountOptions? options = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0");

        options ??= new WaitForEmailCountOptions();
        var timeout = options.Timeout ?? TimeSpan.FromMilliseconds(_options.WaitTimeoutMs);
        var pollInterval = TimeSpan.FromMilliseconds(_options.PollIntervalMs);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        // Ensure subscribed first
        await EnsureSubscribedAsync(pollInterval, linkedCts.Token);

        // Check if we already have enough emails
        var currentCount = await GetEmailCountAsync(linkedCts.Token);
        if (currentCount >= count)
        {
            _logger?.LogDebug(
                "Target email count {TargetCount} already reached in inbox {Email} (current: {CurrentCount})",
                count, EmailAddress, currentCount);
            return;
        }

        _logger?.LogDebug(
            "Waiting for {TargetCount} emails in inbox {Email} (current: {CurrentCount}, timeout: {Timeout}s)",
            count, EmailAddress, currentCount, timeout.TotalSeconds);

        try
        {
            await foreach (var _ in _emailChannel.Reader.ReadAllAsync(linkedCts.Token))
            {
                currentCount = await GetEmailCountAsync(linkedCts.Token);
                _logger?.LogTrace(
                    "Email received, count now {CurrentCount}/{TargetCount}",
                    currentCount, count);

                if (currentCount >= count)
                {
                    _logger?.LogDebug(
                        "Target email count {TargetCount} reached in inbox {Email}",
                        count, EmailAddress);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new VaultSandboxTimeoutException(
                $"Inbox {EmailAddress} did not receive {count} emails within {timeout.TotalSeconds}s (current: {currentCount})",
                timeout);
        }

        // Channel completed without reaching count
        throw new VaultSandboxTimeoutException(
            $"Inbox {EmailAddress} subscription ended before receiving {count} emails (current: {currentCount})",
            timeout);
    }

    private Task EnsureSubscribedWithDefaultsAsync(CancellationToken ct)
    {
        var defaultPollInterval = TimeSpan.FromMilliseconds(_options.PollIntervalMs);
        return EnsureSubscribedAsync(defaultPollInterval, ct);
    }

    private async Task EnsureSubscribedAsync(TimeSpan pollInterval, CancellationToken ct)
    {
        await _subscriptionLock.WaitAsync(ct);
        try
        {
            if (_isSubscribed)
                return;

            await _deliveryStrategy.SubscribeAsync(
                InboxHash,
                EmailAddress,
                OnEmailReceivedAsync,
                pollInterval,
                ct);

            _isSubscribed = true;
            _logger?.LogDebug("Subscribed to inbox {EmailAddress} with poll interval {PollInterval}ms",
                EmailAddress, pollInterval.TotalMilliseconds);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    private async Task OnEmailReceivedAsync(SseEmailEvent emailEvent)
    {
        try
        {
            _logger?.LogDebug("Received email event for {EmailId}", emailEvent.EmailId);

            // Fetch full email data
            var encrypted = await _apiClient.GetEmailAsync(EmailAddress, emailEvent.EmailId);
            var email = await DecryptEmailAsync(encrypted);

            await _emailChannel.Writer.WriteAsync(email);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to process email event {EmailId}", emailEvent.EmailId);
        }
    }

    private async Task<Email> DecryptEmailAsync(
        EmailResponse encrypted,
        CancellationToken ct = default)
    {
        // If encryptedParsed is missing, fetch the full email first
        // (list emails endpoint only returns metadata, not parsed content)
        if (encrypted.EncryptedParsed is null)
        {
            encrypted = await _apiClient.GetEmailAsync(EmailAddress, encrypted.Id, ct);
        }

        // Decrypt metadata
        var metadataBytes = await _cryptoProvider.DecryptAsync(
            encrypted.EncryptedMetadata,
            _keyPair.SecretKey,
            _serverSigPk,
            ct);

        var metadata = System.Text.Json.JsonSerializer.Deserialize(
            metadataBytes,
            VaultSandboxJsonContext.Default.DecryptedMetadata)
            ?? throw new DecryptionException("Failed to deserialize email metadata");

        // Decrypt parsed content
        DecryptedParsed? parsed = null;
        if (encrypted.EncryptedParsed is not null)
        {
            var parsedBytes = await _cryptoProvider.DecryptAsync(
                encrypted.EncryptedParsed,
                _keyPair.SecretKey,
                _serverSigPk,
                ct);

            parsed = System.Text.Json.JsonSerializer.Deserialize(
                parsedBytes,
                VaultSandboxJsonContext.Default.DecryptedParsed);
        }

        return BuildEmail(encrypted, metadata, parsed);
    }

    private Email BuildEmail(
        EmailResponse encrypted,
        DecryptedMetadata metadata,
        DecryptedParsed? parsed)
    {
        // Convert attachments
        IReadOnlyList<EmailAttachment>? attachments = null;
        if (parsed?.Attachments is { Length: > 0 })
        {
            attachments = parsed.Attachments.Select(a => new EmailAttachment
            {
                Filename = a.Filename,
                ContentType = a.ContentType,
                Size = a.Size,
                ContentId = a.ContentId,
                ContentDisposition = a.ContentDisposition,
                Content = Convert.FromBase64String(a.Content),
                Checksum = a.Checksum
            }).ToList();
        }

        // Auth results are now directly deserialized to the Api type
        var authResults = parsed?.AuthResults;

        // Convert headers from JsonElement to object
        IReadOnlyDictionary<string, object>? headers = null;
        if (parsed?.Headers is not null)
        {
            headers = parsed.Headers.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertJsonElement(kvp.Value));
        }

        return new Email(this)
        {
            Id = encrypted.Id,
            InboxId = encrypted.InboxId ?? encrypted.Id, // Fallback to Id if InboxId missing
            From = metadata.From,
            To = metadata.To,
            Subject = metadata.Subject,
            ReceivedAt = encrypted.ReceivedAt ?? metadata.ReceivedAt ?? DateTimeOffset.UtcNow,
            IsRead = encrypted.IsRead,
            Text = parsed?.Text,
            Html = parsed?.Html,
            Headers = headers,
            Attachments = attachments,
            Links = parsed?.Links,
            AuthResults = authResults,
            Metadata = parsed?.Metadata
        };
    }

    private static object ConvertJsonElement(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString()!,
            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null!,
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            System.Text.Json.JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            throw new ObjectDisposedException(nameof(Inbox));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;

        if (_isSubscribed)
        {
            await _deliveryStrategy.UnsubscribeAsync(InboxHash);
        }

        _emailChannel.Writer.Complete();
        _subscriptionLock.Dispose();

        _logger?.LogDebug("Disposed inbox {EmailAddress}", EmailAddress);
    }
}
