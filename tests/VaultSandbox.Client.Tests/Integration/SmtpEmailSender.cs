using MailKit.Net.Smtp;
using MimeKit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Helper class for sending test emails via SMTP with retry logic.
/// </summary>
public sealed class SmtpEmailSender : IDisposable
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private SmtpClient? _client;
    private bool _isConnected;

    public SmtpEmailSender(string smtpHost, int smtpPort, int maxRetries = 5, TimeSpan? initialDelay = null)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(500);
        _client = new SmtpClient();
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_isConnected && _client?.IsConnected == true)
            return;

        await ExecuteWithRetryAsync(async () =>
        {
            // Create a fresh client if the previous one is in a bad state
            if (_client?.IsConnected == false || _isConnected == false)
            {
                _client?.Dispose();
                _client = new SmtpClient();
            }

            await _client!.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.None, ct);
            _isConnected = true;
        }, ct);
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string? textBody = null,
        string? htmlBody = null,
        string? from = null,
        IEnumerable<(string filename, byte[] content, string contentType)>? attachments = null,
        CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Test Sender", from ?? "test@sender.example.com"));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var builder = new BodyBuilder();

        if (textBody != null)
            builder.TextBody = textBody;

        if (htmlBody != null)
            builder.HtmlBody = htmlBody;

        if (attachments != null)
        {
            foreach (var (filename, content, contentType) in attachments)
            {
                var parts = contentType.Split('/');
                builder.Attachments.Add(filename, content, new ContentType(parts[0], parts.Length > 1 ? parts[1] : "octet-stream"));
            }
        }

        message.Body = builder.ToMessageBody();

        await ExecuteWithRetryAsync(async () =>
        {
            await ConnectAsync(ct);
            await _client!.SendAsync(message, ct);
        }, ct);
    }

    private async Task ExecuteWithRetryAsync(Func<Task> action, CancellationToken ct)
    {
        var delay = _initialDelay;

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (SmtpCommandException ex) when (IsTransientError(ex) && attempt < _maxRetries)
            {
                // Reset connection state on transient errors
                _isConnected = false;
                _client?.Dispose();
                _client = new SmtpClient();

                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
            catch (SmtpProtocolException) when (attempt < _maxRetries)
            {
                // Reset connection state on protocol errors
                _isConnected = false;
                _client?.Dispose();
                _client = new SmtpClient();

                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
            catch (IOException) when (attempt < _maxRetries)
            {
                // Reset connection state on IO errors
                _isConnected = false;
                _client?.Dispose();
                _client = new SmtpClient();

                await Task.Delay(delay, ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }

    private static bool IsTransientError(SmtpCommandException ex)
    {
        // SMTP 4xx errors are transient, 421 is "Service not available"
        // Also treat 5xx errors that indicate server overload as transient
        // In test context, also treat recipient rejection as transient since the inbox
        // may not be fully registered on the server yet
        return ex.StatusCode == SmtpStatusCode.ServiceNotAvailable ||
               ex.StatusCode == SmtpStatusCode.ServiceClosingTransmissionChannel ||
               ex.StatusCode == SmtpStatusCode.MailboxBusy ||
               ex.StatusCode == SmtpStatusCode.InsufficientStorage ||
               ex.StatusCode == SmtpStatusCode.ExceededStorageAllocation ||
               ex.StatusCode == SmtpStatusCode.MailboxUnavailable ||
               ex.StatusCode == SmtpStatusCode.UserNotLocalTryAlternatePath ||
               (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500;
    }

    public async Task SendSimpleEmailAsync(
        string to,
        string subject,
        string body,
        string? from = null,
        CancellationToken ct = default)
    {
        await SendEmailAsync(to, subject, textBody: body, from: from, ct: ct);
    }

    public void Dispose()
    {
        if (_client == null)
            return;

        if (_isConnected)
        {
            try
            {
                _client.Disconnect(true);
            }
            catch
            {
                // Ignore disconnect errors
            }
        }
        _client.Dispose();
        _client = null;
    }
}
