<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/logo-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/logo-light.svg">
  <img alt="VaultSandbox" src="./assets/logo-dark.svg">
</picture>

> **VaultSandbox is in Public Beta.** Join the journey to 1.0. Share feedback on [GitHub](https://github.com/vaultsandbox/gateway/discussions).

# VaultSandbox.Client

[![NuGet](https://img.shields.io/nuget/v/VaultSandbox.Client.svg)](https://www.nuget.org/packages/VaultSandbox.Client)
[![CI](https://github.com/vaultsandbox/client-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/vaultsandbox/client-dotnet/actions/workflows/ci.yml)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![.NET](https://img.shields.io/badge/.NET-%3E%3D9.0-512BD4.svg)](https://dotnet.microsoft.com/)

**Production-like email testing. Self-hosted & secure.**

The official .NET SDK for [VaultSandbox Gateway](https://github.com/vaultsandbox/gateway) — a secure, receive-only SMTP server for QA/testing environments. This SDK abstracts encryption complexity, making email testing workflows transparent and effortless.

Stop mocking your email stack. If your app sends real emails in production, it must send real emails in testing. VaultSandbox provides isolated inboxes that behave exactly like production without exposing a single byte of customer data.

> **.NET 9+** required. Not intended for Blazor WebAssembly or browser runtimes.

## Why VaultSandbox?

| Feature             | Simple Mocks     | Public SaaS  | **VaultSandbox**    |
| :------------------ | :--------------- | :----------- | :------------------ |
| **TLS/SSL**         | Ignored/Disabled | Partial      | **Real ACME certs** |
| **Data Privacy**    | Local only       | Shared cloud | **Private VPC**     |
| **Inbound Mail**    | Outbound only    | Yes          | **Real MX**         |
| **Auth (SPF/DKIM)** | None             | Limited      | **Full Validation** |
| **Crypto**          | Plaintext        | Varies       | **Zero-Knowledge**  |

## Features

- **Quantum-Safe Encryption** — Automatic ML-KEM-768 (Kyber768) key encapsulation + AES-256-GCM encryption
- **Zero Crypto Knowledge Required** — All cryptographic operations are invisible to the user
- **Real-Time Email Delivery** — SSE-based delivery for instant updates
- **Built for CI/CD** — Deterministic tests without sleeps, polling, or flakiness
- **Full Email Access** — Decrypt and access email content, headers, links, and attachments
- **Email Authentication** — Built-in SPF/DKIM/DMARC validation helpers
- **Type-Safe** — Full C# type support with comprehensive interfaces and records
- **[Spam Analysis](https://vaultsandbox.dev/client-dotnet/concepts/spam-analysis/)** — Rspamd integration for spam scores, classifications, and rule analysis
- **[Webhooks](https://vaultsandbox.dev/client-dotnet/guides/webhooks/)** — Global and per-inbox HTTP callbacks for email events with filtering and templates
- **[Chaos Engineering](https://vaultsandbox.dev/client-dotnet/guides/chaos/)** — Per-inbox SMTP failure simulation (latency, drops, errors, greylisting, blackhole)

## Installation

```bash
dotnet add package VaultSandbox.Client
```

Or via the NuGet Package Manager:

```powershell
Install-Package VaultSandbox.Client
```

## Quick Start

```csharp
using VaultSandbox.Client;

// Initialize client with your API key
var client = VaultSandboxClientBuilder.Create()
    .WithBaseUrl("https://smtp.vaultsandbox.com")
    .WithApiKey("your-api-key")
    .Build();

await using (client)
{
    // Create inbox (keypair generated automatically)
    var inbox = await client.CreateInboxAsync();
    Console.WriteLine($"Send email to: {inbox.EmailAddress}");

    // Wait for email with timeout
    var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
    {
        Timeout = TimeSpan.FromSeconds(30),
        Subject = "Test",  // Optional filter
    });

    // Email is already decrypted - just use it!
    Console.WriteLine($"From: {email.From}");
    Console.WriteLine($"Subject: {email.Subject}");
    Console.WriteLine($"Text: {email.Text}");
    Console.WriteLine($"HTML: {email.Html}");
}
```

## Usage Examples

### Testing Password Reset Emails

```csharp
using VaultSandbox.Client;

var client = VaultSandboxClientBuilder.Create()
    .WithBaseUrl(url)
    .WithApiKey(apiKey)
    .Build();

await using (client)
{
    var inbox = await client.CreateInboxAsync();

    // Trigger password reset in your app (replace with your own implementation)
    await yourApp.RequestPasswordResetAsync(inbox.EmailAddress);

    // Wait for and validate the reset email
    var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
    {
        Timeout = TimeSpan.FromSeconds(10),
        Subject = "Reset your password",
        UseRegex = true,
    });

    // Extract reset link
    var resetLink = email.Links?.FirstOrDefault(url => url.Contains("/reset-password"));
    Console.WriteLine($"Reset link: {resetLink}");

    // Validate email authentication
    var authValidation = email.AuthResults?.Validate();
    // In a real test, this may not pass if the sender isn't fully configured.
    // A robust check verifies the validation was performed and has the correct shape.
    Assert.IsNotNull(authValidation);
    Assert.IsInstanceOfType<bool>(authValidation.Passed);
    Assert.IsNotNull(authValidation.Failures);
}
```

### Testing Email Authentication (SPF/DKIM/DMARC)

```csharp
var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
{
    Timeout = TimeSpan.FromSeconds(5)
});

var validation = email.AuthResults?.Validate();

if (validation is not null && !validation.Passed)
{
    Console.WriteLine("Email authentication failed:");
    foreach (var reason in validation.Failures)
    {
        Console.WriteLine($"  - {reason}");
    }
}

// Or check individual results. Results can vary based on the sending source.
if (email.AuthResults?.Spf is not null)
{
    Assert.IsTrue(Enum.IsDefined(email.AuthResults.Spf.Result));
}
if (email.AuthResults?.Dkim is not null)
{
    Assert.IsTrue(email.AuthResults.Dkim.Count > 0);
}
if (email.AuthResults?.Dmarc is not null)
{
    Assert.IsTrue(Enum.IsDefined(email.AuthResults.Dmarc.Result));
}
```

### Extracting and Validating Links

```csharp
var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
{
    Subject = "Verify your email",
    UseRegex = true,
});

// All links are automatically extracted
var verifyLink = email.Links?.FirstOrDefault(url => url.Contains("/verify"));
Assert.IsNotNull(verifyLink);
Assert.IsTrue(verifyLink.Contains("https://"));

// Test the verification flow
using var httpClient = new HttpClient();
var response = await httpClient.GetAsync(verifyLink);
Assert.IsTrue(response.IsSuccessStatusCode);
```

### Working with Email Attachments

Email attachments are automatically decrypted and available as `byte[]` arrays, ready to be processed or saved.

```csharp
var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
{
    Subject = "Documents Attached",
    UseRegex = true,
});

// Access attachments array
Console.WriteLine($"Found {email.Attachments?.Count ?? 0} attachments");

// Iterate through attachments
if (email.Attachments is not null)
{
    foreach (var attachment in email.Attachments)
    {
        Console.WriteLine($"Filename: {attachment.Filename}");
        Console.WriteLine($"Content-Type: {attachment.ContentType}");
        Console.WriteLine($"Size: {attachment.Size} bytes");

        if (attachment.Content is null) continue;

        // Decode text-based attachments
        if (attachment.ContentType.Contains("text"))
        {
            var textContent = System.Text.Encoding.UTF8.GetString(attachment.Content);
            Console.WriteLine($"Content: {textContent}");
        }

        // Parse JSON attachments
        if (attachment.ContentType.Contains("json"))
        {
            var jsonContent = System.Text.Encoding.UTF8.GetString(attachment.Content);
            var data = System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent);
            Console.WriteLine($"Parsed data: {data}");
        }

        // Save binary files to disk
        if (attachment.ContentType.Contains("pdf") || attachment.ContentType.Contains("image"))
        {
            await File.WriteAllBytesAsync($"./downloads/{attachment.Filename}", attachment.Content);
            Console.WriteLine($"Saved {attachment.Filename}");
        }
    }
}

// Find and verify specific attachment in tests
var pdfAttachment = email.Attachments?.FirstOrDefault(att => att.Filename == "invoice.pdf");
Assert.IsNotNull(pdfAttachment);
Assert.AreEqual("application/pdf", pdfAttachment.ContentType);
Assert.IsTrue(pdfAttachment.Size > 0);

// Verify attachment content exists and has expected size
if (pdfAttachment.Content is not null)
{
    Assert.AreEqual(pdfAttachment.Size, pdfAttachment.Content.Length);
}
```

### Testing with xUnit

```csharp
public class EmailFlowTests : IAsyncLifetime
{
    private IVaultSandboxClient _client = null!;
    private IInbox _inbox = null!;

    public async Task InitializeAsync()
    {
        _client = VaultSandboxClientBuilder.Create()
            .WithBaseUrl(url)
            .WithApiKey(apiKey)
            .Build();
        _inbox = await _client.CreateInboxAsync();
    }

    public async Task DisposeAsync()
    {
        if (_inbox is not null)
            await _inbox.DisposeAsync();
        if (_client is not null)
            await _client.DisposeAsync();
    }

    [Fact]
    public async Task Should_receive_welcome_email()
    {
        await SendWelcomeEmailAsync(_inbox.EmailAddress);

        var email = await _inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Subject = "Welcome",
            UseRegex = true,
        });

        Assert.Equal("noreply@example.com", email.From);
        Assert.Contains("Thank you for signing up", email.Text);
    }
}
```

### Testing with NUnit

```csharp
[TestFixture]
public class EmailFlowTests
{
    private IVaultSandboxClient _client = null!;
    private IInbox _inbox = null!;

    [SetUp]
    public async Task SetUp()
    {
        _client = VaultSandboxClientBuilder.Create()
            .WithBaseUrl(url)
            .WithApiKey(apiKey)
            .Build();
        _inbox = await _client.CreateInboxAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_inbox is not null)
            await _inbox.DisposeAsync();
        if (_client is not null)
            await _client.DisposeAsync();
    }

    [Test]
    public async Task Should_receive_welcome_email()
    {
        await SendWelcomeEmailAsync(_inbox.EmailAddress);

        var email = await _inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            Subject = "Welcome",
            UseRegex = true,
        });

        Assert.That(email.From, Is.EqualTo("noreply@example.com"));
        Assert.That(email.Text, Does.Contain("Thank you for signing up"));
    }
}
```

### Waiting for Multiple Emails

When testing scenarios that send multiple emails, use `WaitForEmailCountAsync()` instead of arbitrary delays for faster and more reliable tests:

```csharp
[Fact]
public async Task Should_receive_multiple_notification_emails()
{
    // Send multiple emails
    await SendNotificationsAsync(_inbox.EmailAddress, 3);

    // Wait for all 3 emails to arrive
    await _inbox.WaitForEmailCountAsync(3, new WaitForEmailCountOptions
    {
        Timeout = TimeSpan.FromSeconds(30)
    });

    // Now list and verify all emails
    var emails = await _inbox.GetEmailsAsync();
    Assert.Equal(3, emails.Count);
    Assert.Contains("Notification", emails[0].Subject);
}
```

### Real-time Monitoring with IAsyncEnumerable

For scenarios where you need to process emails as they arrive without blocking, you can use the `WatchAsync` method which returns an `IAsyncEnumerable<Email>`.

```csharp
using VaultSandbox.Client;

var client = VaultSandboxClientBuilder.Create()
    .WithBaseUrl(url)
    .WithApiKey(apiKey)
    .Build();

await using (client)
{
    var inbox = await client.CreateInboxAsync();
    Console.WriteLine($"Watching for emails at: {inbox.EmailAddress}");

    using var cts = new CancellationTokenSource();

    // Process emails as they arrive
    await foreach (var email in inbox.WatchAsync(cts.Token))
    {
        Console.WriteLine($"New email received: \"{email.Subject}\"");
        // Process the email here...
    }
}
```

### Monitoring Multiple Inboxes

```csharp
var inbox1 = await client.CreateInboxAsync();
var inbox2 = await client.CreateInboxAsync();

var monitor = client.MonitorInboxes(inbox1, inbox2);

Console.WriteLine($"Monitoring inboxes: {inbox1.EmailAddress}, {inbox2.EmailAddress}");

await foreach (var evt in monitor.WatchAsync())
{
    Console.WriteLine($"New email in {evt.InboxAddress}: {evt.Email.Subject}");
    // Further processing...
}
```

### Dependency Injection

The SDK integrates seamlessly with the .NET dependency injection container.

**Using IConfiguration:**

```csharp
// appsettings.json
{
    "VaultSandbox": {
        "BaseUrl": "https://smtp.vaultsandbox.com",
        "ApiKey": "your-api-key"
    }
}

// Program.cs or Startup.cs
services.AddVaultSandboxClient(configuration);
```

**Using a configuration action:**

```csharp
services.AddVaultSandboxClient(options =>
{
    options.BaseUrl = "https://smtp.vaultsandbox.com";
    options.ApiKey = Environment.GetEnvironmentVariable("VAULTSANDBOX_API_KEY")!;
    options.DefaultDeliveryStrategy = DeliveryStrategy.Sse;
});
```

**Using the builder with service provider:**

```csharp
services.AddVaultSandboxClient((builder, sp) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    builder
        .WithBaseUrl(config["VaultSandbox:BaseUrl"]!)
        .WithApiKey(config["VaultSandbox:ApiKey"]!)
        .WithLogging(sp.GetRequiredService<ILoggerFactory>());
});
```

Then inject `IVaultSandboxClient` where needed:

```csharp
public class EmailTestService
{
    private readonly IVaultSandboxClient _client;

    public EmailTestService(IVaultSandboxClient client)
    {
        _client = client;
    }

    public async Task TestPasswordResetAsync()
    {
        var inbox = await _client.CreateInboxAsync();
        // ...
    }
}
```

## API Reference

### VaultSandboxClientBuilder

The fluent builder for creating `IVaultSandboxClient` instances.

#### Factory Method

```csharp
VaultSandboxClientBuilder.Create()
```

#### Configuration Methods

- `WithBaseUrl(string baseUrl)` - Gateway URL (required)
- `WithApiKey(string apiKey)` - Your API key (required)
- `WithHttpTimeout(TimeSpan timeout)` - HTTP request timeout (default: 30s)
- `WithWaitTimeout(TimeSpan timeout)` - Default WaitForEmail timeout (default: 30s)
- `WithPollInterval(TimeSpan interval)` - Polling interval (default: 2s)
- `WithMaxRetries(int maxRetries)` - Max retry attempts (default: 3)
- `WithRetryDelay(TimeSpan delay)` - Initial retry delay (default: 1s)
- `WithSseReconnectInterval(TimeSpan interval)` - SSE reconnection interval (default: 5s)
- `WithSseMaxReconnectAttempts(int maxAttempts)` - Max SSE reconnection attempts (default: 10)
- `WithDeliveryStrategy(DeliveryStrategy strategy)` - Delivery strategy
- `UseSseDelivery()` - Use SSE delivery strategy (default)
- `UsePollingDelivery()` - Use polling delivery strategy
- `WithDefaultInboxTtl(TimeSpan ttl)` - Default inbox TTL (default: 1 hour)
- `WithLogging(ILoggerFactory loggerFactory)` - Configure logging
- `WithHttpClient(HttpClient httpClient, bool disposeClient = false)` - Use custom HttpClient

#### Build Methods

- `Build()` - Creates the client
- `BuildAndValidateAsync(CancellationToken ct = default)` - Creates and validates the client

### IVaultSandboxClient

The main client interface for interacting with the VaultSandbox Gateway.

#### Methods

- `CreateInboxAsync(CreateInboxOptions? options = null, CancellationToken ct = default)` - Creates a new inbox
- `DeleteInboxAsync(string emailAddress, CancellationToken ct = default)` - Deletes a single inbox
- `DeleteAllInboxesAsync(CancellationToken ct = default)` - Deletes all inboxes for this API key
- `ValidateApiKeyAsync(CancellationToken ct = default)` - Validates API key
- `GetServerInfoAsync(CancellationToken ct = default)` - Gets server information
- `MonitorInboxes(params IInbox[] inboxes)` - Monitors multiple inboxes
- `ExportInboxToFileAsync(IInbox inbox, string filePath, CancellationToken ct = default)` - Exports inbox to JSON file
- `ImportInboxFromFileAsync(string filePath, CancellationToken ct = default)` - Imports inbox from JSON file
- `ImportInboxAsync(InboxExport export, CancellationToken ct = default)` - Imports inbox from export data

### IInbox

Represents a single email inbox.

#### Properties

- `EmailAddress` - The inbox email address
- `InboxHash` - Unique inbox identifier
- `ExpiresAt` - When the inbox expires
- `IsDisposed` - Whether disposed

#### Methods

- `GetEmailsAsync(CancellationToken ct = default)` - Lists all emails (decrypted)
- `GetEmailAsync(string emailId, CancellationToken ct = default)` - Gets a specific email
- `GetEmailRawAsync(string emailId, CancellationToken ct = default)` - Gets raw email source
- `WaitForEmailAsync(WaitForEmailOptions? options = null, CancellationToken ct = default)` - Waits for an email matching criteria
- `WaitForEmailCountAsync(int count, WaitForEmailCountOptions? options = null, CancellationToken ct = default)` - Waits until inbox has N emails
- `WatchAsync(CancellationToken ct = default)` - Watches for new emails as `IAsyncEnumerable<Email>`
- `GetEmailCountAsync(CancellationToken ct = default)` - Gets current email count
- `GetSyncStatusAsync(CancellationToken ct = default)` - Gets inbox sync status
- `MarkAsReadAsync(string emailId, CancellationToken ct = default)` - Marks email as read
- `DeleteEmailAsync(string emailId, CancellationToken ct = default)` - Deletes an email
- `ExportAsync()` - Exports inbox data

### Email

Represents a decrypted email.

#### Properties

- `Id` - Email ID
- `InboxId` - The inbox hash this email belongs to
- `From` - Sender address
- `To` - Recipient addresses
- `Subject` - Email subject
- `Text` - Plain text content
- `Html` - HTML content
- `ReceivedAt` - When the email was received
- `IsRead` - Read status
- `Links` - Extracted URLs from email
- `Headers` - All email headers
- `Attachments` - Email attachments
- `AuthResults` - Email authentication results
- `Metadata` - Other metadata

#### Methods

- `MarkAsReadAsync(CancellationToken ct = default)` - Marks this email as read
- `DeleteAsync(CancellationToken ct = default)` - Deletes this email
- `GetRawAsync(CancellationToken ct = default)` - Gets raw email source

### EmailAttachment

Represents an email attachment.

#### Properties

- `Filename` - Attachment filename
- `ContentType` - MIME type (e.g., "application/pdf")
- `Size` - Size in bytes
- `Content` - Decoded binary content as `byte[]`
- `ContentId` - Content ID for inline attachments
- `ContentDisposition` - "attachment" or "inline"
- `Checksum` - Optional SHA-256 checksum

### AuthenticationResults

Email authentication results (SPF, DKIM, DMARC).

#### Properties

- `Spf` - SPF result
- `Dkim` - All DKIM results
- `Dmarc` - DMARC result
- `ReverseDns` - Reverse DNS result

#### Methods

- `Validate()` - Returns `AuthValidation` with `Passed`, per-check booleans, and `Failures` list

### CreateInboxOptions

Options for creating an inbox.

#### Properties

- `EmailAddress` - Optional specific email address to request
- `Ttl` - Time-to-live for the inbox (default: 1 hour)

### WaitForEmailOptions

Options for waiting for emails.

#### Properties

- `Timeout` - Maximum time to wait (default: 30s)
- `PollInterval` - Polling interval (default: 2s)
- `Subject` - Filter emails by subject (exact or regex)
- `From` - Filter emails by sender address (exact or regex)
- `Predicate` - Custom filter function `Func<Email, bool>`
- `UseRegex` - Whether to use regex matching (default: false)

### WaitForEmailCountOptions

Options for waiting for a specific number of emails.

#### Properties

- `Timeout` - Maximum time to wait (default: 30s)

### InboxMonitor

Monitors multiple inboxes for new emails.

#### Properties

- `Inboxes` - The monitored inboxes
- `InboxCount` - Number of monitored inboxes

#### Methods

- `WatchAsync(CancellationToken ct = default)` - Returns `IAsyncEnumerable<InboxEmailEvent>`
- `Start()` - Explicitly start monitoring

### InboxEmailEvent

Event emitted when an email arrives in a monitored inbox.

#### Properties

- `Inbox` - The inbox that received the email
- `Email` - The received email
- `InboxAddress` - Shortcut to inbox email address

## Error Handling

The SDK is designed to be resilient and provide clear feedback when issues occur. It includes automatic retries for transient network and server errors, and throws specific, catchable exceptions for different failure scenarios.

All custom exceptions thrown by the SDK inherit from the base `VaultSandboxException` class, so you can catch all SDK-specific errors with a single `catch` block if needed.

### Automatic Retries

By default, the client automatically retries failed HTTP requests that result in one of the following status codes: `408`, `429`, `500`, `502`, `503`, `504`. This helps mitigate transient network or server-side issues.

The retry behavior can be configured via the `VaultSandboxClientBuilder`:

- `WithMaxRetries(int)` - The maximum number of retry attempts (default: 3)
- `WithRetryDelay(TimeSpan)` - The base delay between retries (default: 1s). Uses exponential backoff.

### Exception Types

| Exception | Key Properties | Purpose |
|-----------|----------------|---------|
| `VaultSandboxException` | Message, InnerException | Base class for all SDK errors |
| `ApiException` | `StatusCode`, `ResponseBody` | HTTP API errors |
| `VaultSandboxTimeoutException` | `Timeout` | Operation timeouts |
| `DecryptionException` | Message | Decryption failures (CRITICAL) |
| `SignatureVerificationException` | Message | Signature verification failures (CRITICAL) |
| `EmailNotFoundException` | `EmailId` | Email not found (404) |
| `InboxNotFoundException` | `EmailAddress` | Inbox not found (404) |
| `InboxAlreadyExistsException` | `EmailAddress` | Inbox already exists during import |
| `InvalidImportDataException` | Message | Import data validation failures |
| `NetworkException` | Message | Network connectivity issues |
| `SseException` | Message | SSE connection issues |

### Example

```csharp
using VaultSandbox.Client;
using VaultSandbox.Client.Exceptions;

var client = VaultSandboxClientBuilder.Create()
    .WithBaseUrl(url)
    .WithApiKey(apiKey)
    .Build();

try
{
    await using (client)
    {
        var inbox = await client.CreateInboxAsync();
        Console.WriteLine($"Send email to: {inbox.EmailAddress}");

        // This might throw a VaultSandboxTimeoutException
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(5)
        });

        Console.WriteLine($"Email received: {email.Subject}");
    }
}
catch (VaultSandboxTimeoutException ex)
{
    Console.WriteLine($"Timed out waiting for email after {ex.Timeout}: {ex.Message}");
}
catch (ApiException ex)
{
    Console.WriteLine($"API Error ({ex.StatusCode}): {ex.Message}");
}
catch (VaultSandboxException ex)
{
    // Catch any other SDK-specific error
    Console.WriteLine($"An unexpected SDK error occurred: {ex.Message}");
}
```

## Configuration Options

### VaultSandboxClientOptions

All configuration options available when using dependency injection:

```csharp
public sealed class VaultSandboxClientOptions
{
    public required string BaseUrl { get; set; }
    public required string ApiKey { get; set; }
    public int HttpTimeoutMs { get; set; } = 30_000;
    public int WaitTimeoutMs { get; set; } = 30_000;
    public int PollIntervalMs { get; set; } = 2_000;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1_000;
    public int SseReconnectIntervalMs { get; set; } = 5_000;
    public int SseMaxReconnectAttempts { get; set; } = 10;
    public DeliveryStrategy DefaultDeliveryStrategy { get; set; } = DeliveryStrategy.Sse;
    public int DefaultInboxTtlSeconds { get; set; } = 3600;
}
```

### DeliveryStrategy

```csharp
public enum DeliveryStrategy
{
    Sse,        // Server-Sent Events (default)
    Polling     // Polling
}
```

## Requirements

- .NET 9.0 or later
- Not supported in Blazor WebAssembly or browser runtimes
- VaultSandbox Gateway server
- Valid API key

## Building

```bash
dotnet build
```

## Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
~/.dotnet/tools/reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;TextSummary"

# View summary
cat ./coverage/report/Summary.txt
```

## Architecture

The SDK is built on several layers:

1. **Crypto Layer**: Handles ML-KEM-768 keypair generation, AES-256-GCM encryption/decryption, and ML-DSA-65 signature verification
2. **HTTP Layer**: REST API client with automatic retry and error handling
3. **Domain Layer**: Email, Inbox, and Client classes with intuitive APIs
4. **Strategy Layer**: SSE and polling strategies for email delivery

All cryptographic operations are performed transparently - developers never need to handle keys, encryption, or signatures directly.

## Security

- **Cryptography**: ML-KEM-768 (Kyber768) for key encapsulation + AES-256-GCM for payload encryption, with HKDF-SHA-512 key derivation.
- **Signatures**: ML-DSA-65 (Dilithium3) signatures are verified **before** any decryption using the gateway-provided transcript context (`vaultsandbox:email:v1` today).
- **Threat model**: Protects confidentiality/integrity of gateway responses and detects tampering/MITM. Skipping signature verification defeats these guarantees.
- **Key handling**: Inbox keypairs stay in memory only; exported inbox data contains secrets and must be treated as sensitive.
- **Validation**: Signature verification failures throw `SignatureVerificationException`; decryption issues throw `DecryptionException`. Always surface these in logs/alerts for investigation.

## Related

- [VaultSandbox Gateway](https://github.com/vaultsandbox/gateway) — The self-hosted SMTP server this SDK connects to
- [VaultSandbox Documentation](https://vaultsandbox.dev) — Full documentation and guides

## Support

- [Documentation](https://vaultsandbox.dev/client-dotnet/)
- [Issue Tracker](https://github.com/vaultsandbox/client-dotnet/issues)
- [Discussions](https://github.com/vaultsandbox/gateway/discussions)
- [Website](https://www.vaultsandbox.com)

## Contributing

Contributions are welcome! Please read our [contributing guidelines](CONTRIBUTING.md) before submitting PRs.

## License

Apache 2.0 — see [LICENSE](LICENSE) for details.
