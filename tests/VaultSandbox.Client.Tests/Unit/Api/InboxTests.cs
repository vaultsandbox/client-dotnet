using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class InboxTests
{
    private const string TestEmailAddress = "test@vaultsandbox.dev";
    private const string TestInboxHash = "test-inbox-hash";
    private const string TestServerSigPk = "test-server-sig-pk";

    private static readonly byte[] TestPublicKey = new byte[1184];
    private static readonly byte[] TestSecretKey = new byte[2400];

    private readonly Mock<IVaultSandboxApiClient> _mockApiClient;
    private readonly Mock<ICryptoProvider> _mockCryptoProvider;
    private readonly Mock<IDeliveryStrategy> _mockDeliveryStrategy;
    private readonly VaultSandboxClientOptions _options;
    private readonly MlKemKeyPair _keyPair;

    public InboxTests()
    {
        _mockApiClient = new Mock<IVaultSandboxApiClient>();
        _mockCryptoProvider = new Mock<ICryptoProvider>();
        _mockDeliveryStrategy = new Mock<IDeliveryStrategy>();
        _options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://api.vaultsandbox.dev",
            ApiKey = "test-api-key",
            WaitTimeoutMs = 5000,
            PollIntervalMs = 100
        };
        _keyPair = new MlKemKeyPair
        {
            PublicKey = TestPublicKey,
            SecretKey = TestSecretKey
        };
    }

    private Inbox CreateInbox(ILogger<Inbox>? logger = null)
    {
        return new Inbox(
            TestEmailAddress,
            DateTimeOffset.UtcNow.AddHours(1),
            TestInboxHash,
            emailAuth: true,
            TestServerSigPk,
            _keyPair,
            _mockApiClient.Object,
            _mockCryptoProvider.Object,
            _mockDeliveryStrategy.Object,
            _options,
            logger);
    }

    private static EncryptedPayload CreateEncryptedPayload() => new()
    {
        Version = 1,
        Algorithms = new AlgorithmSuite
        {
            Kem = "ML-KEM-768",
            Sig = "ML-DSA-65",
            Aead = "AES-256-GCM",
            Kdf = "HKDF-SHA256"
        },
        CtKem = "test-ct-kem",
        Nonce = "test-nonce",
        Aad = "test-aad",
        Ciphertext = "test-ciphertext",
        Signature = "test-signature",
        ServerSigPk = TestServerSigPk
    };

    private static EmailResponse CreateEmailResponse(string id = "email-1") => new()
    {
        Id = id,
        InboxId = TestInboxHash,
        ReceivedAt = DateTimeOffset.UtcNow,
        IsRead = false,
        EncryptedMetadata = CreateEncryptedPayload(),
        EncryptedParsed = CreateEncryptedPayload()
    };

    private void SetupDecryption(string from = "sender@example.com", string subject = "Test Subject")
    {
        var metadata = new DecryptedMetadata
        {
            From = from,
            To = [TestEmailAddress],
            Subject = subject,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            Html = "<p>Test body HTML</p>"
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EncryptedPayload payload, byte[] _, string _, CancellationToken _) =>
                // Return metadata for first call, parsed for second
                payload == CreateEmailResponse().EncryptedMetadata ? metadataBytes : parsedBytes);

        // More specific setup for metadata decryption
        _mockCryptoProvider
            .SetupSequence(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadataBytes)
            .ReturnsAsync(parsedBytes);
    }

    #region Constructor and Properties Tests

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var inbox = new Inbox(
            TestEmailAddress,
            expiresAt,
            TestInboxHash,
            emailAuth: true,
            TestServerSigPk,
            _keyPair,
            _mockApiClient.Object,
            _mockCryptoProvider.Object,
            _mockDeliveryStrategy.Object,
            _options);

        // Assert
        inbox.EmailAddress.Should().Be(TestEmailAddress);
        inbox.ExpiresAt.Should().Be(expiresAt);
        inbox.InboxHash.Should().Be(TestInboxHash);
        inbox.IsDisposed.Should().BeFalse();
    }

    #endregion

    #region GetEmailsAsync Tests

    [Fact]
    public async Task GetEmailsAsync_ReturnsDecryptedEmails()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = new[] { CreateEmailResponse("email-1"), CreateEmailResponse("email-2") };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        SetupDecryptionForMultipleEmails();

        // Act
        var emails = await inbox.GetEmailsAsync();

        // Assert
        emails.Should().HaveCount(2);
        _mockDeliveryStrategy.Verify(x => x.SubscribeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Func<SseEmailEvent, Task>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<Func<Task>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEmailsAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetEmailsAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region GetEmailsMetadataOnlyAsync Tests

    [Fact]
    public async Task GetEmailsMetadataOnlyAsync_ReturnsMetadataOnly()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = new[] { CreateEmailResponse() };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        SetupMetadataOnlyDecryption();

        // Act
        var metadata = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadata.Should().HaveCount(1);
        metadata[0].From.Should().Be("sender@example.com");
    }

    [Fact]
    public async Task GetEmailsMetadataOnlyAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetEmailsMetadataOnlyAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region GetEmailAsync Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsDecryptedEmail()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "test-email-id";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionForMultipleEmails();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Id.Should().Be(emailId);
        email.From.Should().Be("sender@example.com");
    }

    [Fact]
    public async Task GetEmailAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetEmailAsync("any-id");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region GetEmailRawAsync Tests

    [Fact]
    public async Task GetEmailRawAsync_ReturnsDecryptedRawEmail()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "test-email-id";
        var rawContent = "From: sender@example.com\r\nTo: test@example.com\r\nSubject: Test\r\n\r\nBody";
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawContent));
        var rawBytes = Encoding.UTF8.GetBytes(base64Content);

        var rawResponse = new RawEmailResponse
        {
            Id = emailId,
            EncryptedRaw = CreateEncryptedPayload()
        };

        _mockApiClient
            .Setup(x => x.GetRawEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawBytes);

        // Act
        var raw = await inbox.GetEmailRawAsync(emailId);

        // Assert
        raw.Should().Be(rawContent);
    }

    [Fact]
    public async Task GetEmailRawAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetEmailRawAsync("any-id");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_CallsApiClient()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "test-email-id";

        // Act
        await inbox.MarkAsReadAsync(emailId);

        // Assert
        _mockApiClient.Verify(
            x => x.MarkEmailAsReadAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.MarkAsReadAsync("any-id");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region DeleteEmailAsync Tests

    [Fact]
    public async Task DeleteEmailAsync_CallsApiClient()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "test-email-id";

        // Act
        await inbox.DeleteEmailAsync(emailId);

        // Assert
        _mockApiClient.Verify(
            x => x.DeleteEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEmailAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.DeleteEmailAsync("any-id");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region ExportAsync Tests

    [Fact]
    public async Task ExportAsync_ReturnsValidExport()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var inbox = new Inbox(
            TestEmailAddress,
            expiresAt,
            TestInboxHash,
            emailAuth: true,
            TestServerSigPk,
            _keyPair,
            _mockApiClient.Object,
            _mockCryptoProvider.Object,
            _mockDeliveryStrategy.Object,
            _options);

        // Act
        var export = await inbox.ExportAsync();

        // Assert
        export.Version.Should().Be(1);
        export.EmailAddress.Should().Be(TestEmailAddress);
        export.ExpiresAt.Should().Be(expiresAt);
        export.InboxHash.Should().Be(TestInboxHash);
        export.ServerSigPk.Should().Be(TestServerSigPk);
        export.SecretKey.Should().Be(Base64Url.Encode(TestSecretKey));
        export.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.ExportAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region GetEmailCountAsync Tests

    [Fact]
    public async Task GetEmailCountAsync_ReturnsCount()
    {
        // Arrange
        var inbox = CreateInbox();
        var syncResponse = new InboxSyncResponse
        {
            EmailCount = 5,
            EmailsHash = "test-hash"
        };

        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResponse);

        // Act
        var count = await inbox.GetEmailCountAsync();

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task GetEmailCountAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetEmailCountAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region GetSyncStatusAsync Tests

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsSyncStatus()
    {
        // Arrange
        var inbox = CreateInbox();
        var syncResponse = new InboxSyncResponse
        {
            EmailCount = 3,
            EmailsHash = "abc123"
        };

        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResponse);

        // Act
        var status = await inbox.GetSyncStatusAsync();

        // Assert
        status.EmailCount.Should().Be(3);
        status.EmailsHash.Should().Be("abc123");
    }

    [Fact]
    public async Task GetSyncStatusAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.GetSyncStatusAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region WaitForEmailCountAsync Tests

    [Fact]
    public async Task WaitForEmailCountAsync_ThrowsWhenCountIsZeroOrNegative()
    {
        // Arrange
        var inbox = CreateInbox();

        // Act & Assert
        var action = () => inbox.WaitForEmailCountAsync(0);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();

        action = () => inbox.WaitForEmailCountAsync(-1);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WaitForEmailCountAsync_ReturnsImmediatelyWhenCountReached()
    {
        // Arrange
        var inbox = CreateInbox();
        var syncResponse = new InboxSyncResponse
        {
            EmailCount = 5,
            EmailsHash = "test-hash"
        };

        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(syncResponse);

        // Act & Assert - should complete without timeout
        await inbox.WaitForEmailCountAsync(3, new WaitForEmailCountOptions { Timeout = TimeSpan.FromMilliseconds(100) });
    }

    [Fact]
    public async Task WaitForEmailCountAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.WaitForEmailCountAsync(1);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task WaitForEmailCountAsync_WaitsForEmailsViaChannel()
    {
        // Arrange - This test covers lines 285-305 (the waiting loop)
        var inbox = CreateInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        // Setup delivery strategy to capture the onEmail callback
        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        // Initial count is 0, we want 2 emails
        var syncCallCount = 0;
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new InboxSyncResponse
            {
                EmailCount = syncCallCount++,
                EmailsHash = $"hash-{syncCallCount}"
            });

        // Setup email fetching for when emails arrive via channel
        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string emailId, CancellationToken _) => CreateEmailResponse(emailId));

        SetupDecryptionForMultipleEmails();

        // Act - Start waiting in background
        var waitTask = inbox.WaitForEmailCountAsync(2, new WaitForEmailCountOptions
        {
            Timeout = TimeSpan.FromSeconds(5)
        });

        // Give time for subscription to happen
        await Task.Delay(50);

        // Simulate emails arriving via the delivery strategy
        capturedOnEmail.Should().NotBeNull();
        await capturedOnEmail!(new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "email-1",
            EncryptedMetadata = CreateEncryptedPayload()
        });
        await capturedOnEmail(new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "email-2",
            EncryptedMetadata = CreateEncryptedPayload()
        });

        // Assert - Should complete without timeout
        await waitTask;
    }

    [Fact]
    public async Task WaitForEmailCountAsync_ThrowsTimeoutWhenCountNotReached()
    {
        // Arrange - This test covers lines 307-312 (timeout exception)
        var inbox = CreateInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        // Always return count of 0
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = "empty-hash" });

        // Act & Assert
        var action = () => inbox.WaitForEmailCountAsync(5, new WaitForEmailCountOptions
        {
            Timeout = TimeSpan.FromMilliseconds(100)
        });

        var exception = await action.Should().ThrowAsync<VaultSandboxTimeoutException>();
        exception.Which.Message.Should().Contain("did not receive 5 emails");
    }

    [Fact]
    public async Task WaitForEmailCountAsync_ReachesTargetCountDuringWait()
    {
        // Arrange - This test covers the success path in the waiting loop (lines 298-303)
        var inbox = CreateInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        // Start with 1 email, need 3
        var emailCount = 1;
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new InboxSyncResponse
            {
                EmailCount = emailCount,
                EmailsHash = $"hash-{emailCount}"
            });

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string emailId, CancellationToken _) => CreateEmailResponse(emailId));

        SetupDecryptionForMultipleEmails();

        // Act
        var waitTask = inbox.WaitForEmailCountAsync(3, new WaitForEmailCountOptions
        {
            Timeout = TimeSpan.FromSeconds(5)
        });

        await Task.Delay(50);

        // Simulate emails arriving - increase count each time
        capturedOnEmail.Should().NotBeNull();
        emailCount = 2;
        await capturedOnEmail!(new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "email-2",
            EncryptedMetadata = CreateEncryptedPayload()
        });

        emailCount = 3;
        await capturedOnEmail(new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "email-3",
            EncryptedMetadata = CreateEncryptedPayload()
        });

        // Assert - Should complete successfully
        await waitTask;
    }

    #endregion

    #region WaitForEmailAsync Tests

    [Fact]
    public async Task WaitForEmailAsync_ReturnsMatchingExistingEmail()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = new[] { CreateEmailResponse() };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        SetupDecryptionForMultipleEmails();

        // Act
        var options = new WaitForEmailOptions
        {
            Subject = "Test Subject",
            Timeout = TimeSpan.FromMilliseconds(500)
        };
        var email = await inbox.WaitForEmailAsync(options);

        // Assert
        email.Should().NotBeNull();
        email.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public async Task WaitForEmailAsync_ThrowsTimeoutWhenNoMatchingEmail()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = Array.Empty<EmailResponse>();

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        // Act
        var options = new WaitForEmailOptions
        {
            Subject = "Nonexistent",
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        var action = () => inbox.WaitForEmailAsync(options);
        await action.Should().ThrowAsync<VaultSandboxTimeoutException>();
    }

    [Fact]
    public async Task WaitForEmailAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = () => inbox.WaitForEmailAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region WatchAsync Tests

    [Fact]
    public async Task WatchAsync_ThrowsWhenDisposed()
    {
        // Arrange
        var inbox = CreateInbox();
        await inbox.DisposeAsync();

        // Act & Assert
        var action = async () =>
        {
            await foreach (var _ in inbox.WatchAsync())
            {
                // Should not reach here
            }
        };
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_UnsubscribesFromDeliveryStrategy()
    {
        // Arrange
        var inbox = CreateInbox();

        // First subscribe
        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        // Trigger subscription by calling GetEmailsAsync
        await inbox.GetEmailsAsync();

        // Act
        await inbox.DisposeAsync();

        // Assert
        _mockDeliveryStrategy.Verify(
            x => x.UnsubscribeAsync(TestInboxHash),
            Times.Once);
        inbox.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var inbox = CreateInbox();

        // Act & Assert - should not throw
        await inbox.DisposeAsync();
        await inbox.DisposeAsync();
        await inbox.DisposeAsync();

        inbox.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_DoesNotUnsubscribeIfNeverSubscribed()
    {
        // Arrange
        var inbox = CreateInbox();

        // Act - dispose without ever subscribing
        await inbox.DisposeAsync();

        // Assert
        _mockDeliveryStrategy.Verify(
            x => x.UnsubscribeAsync(It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Email with Attachments Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithAttachments()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-attachments";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithAttachments();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Attachments.Should().NotBeNull();
        email.Attachments.Should().HaveCount(1);
        email.Attachments![0].Filename.Should().Be("test.txt");
        email.Attachments[0].ContentType.Should().Be("text/plain");
        email.Attachments[0].Size.Should().Be(11);
        email.Attachments[0].ContentId.Should().Be("content-123");
        email.Attachments[0].ContentDisposition.Should().Be("attachment");
        email.Attachments[0].Content.Should().NotBeEmpty();
        email.Attachments[0].Checksum.Should().Be("abc123");
    }

    #endregion

    #region Email with Headers Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithHeaders()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-headers";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithHeaders();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Headers.Should().NotBeNull();
        email.Headers!["X-Custom-Header"].Should().Be("custom-value");
        email.Headers["X-Priority"].Should().Be(1.0);
        email.Headers["X-Boolean-True"].Should().Be(true);
        email.Headers["X-Boolean-False"].Should().Be(false);
    }

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithComplexHeaders()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-complex-headers";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithComplexHeaders();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Headers.Should().NotBeNull();
        // Array header
        var arrayHeader = email.Headers!["X-Array-Header"] as List<object>;
        arrayHeader.Should().NotBeNull();
        arrayHeader.Should().Contain("item1");
        // Object header
        var objectHeader = email.Headers["X-Object-Header"] as Dictionary<string, object>;
        objectHeader.Should().NotBeNull();
        objectHeader!["key"].Should().Be("value");
    }

    #endregion

    #region Email with Auth Results Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithAuthResults()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-auth";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithAuthResults();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.AuthResults.Should().NotBeNull();
        email.AuthResults!.Spf.Should().NotBeNull();
        email.AuthResults.Spf!.Result.Should().Be(SpfStatus.Pass);
        email.AuthResults.Dkim.Should().NotBeNull();
        email.AuthResults.Dkim.Should().HaveCount(1);
        email.AuthResults.Dkim![0].Result.Should().Be(DkimStatus.Pass);
        email.AuthResults.Dmarc.Should().NotBeNull();
        email.AuthResults.Dmarc!.Result.Should().Be(DmarcStatus.Pass);
    }

    #endregion

    #region Email with Links Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithLinks()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-links";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithLinks();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Links.Should().NotBeNull();
        email.Links.Should().Contain("https://example.com");
        email.Links.Should().Contain("https://test.com");
    }

    #endregion

    #region Email Fallback Tests

    [Fact]
    public async Task GetEmailAsync_UsesIdAsInboxIdWhenNull()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-without-inbox-id";
        var response = new EmailResponse
        {
            Id = emailId,
            InboxId = null, // InboxId is null
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            EncryptedMetadata = CreateEncryptedPayload(),
            EncryptedParsed = CreateEncryptedPayload()
        };

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SetupDecryptionForMultipleEmails();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.InboxId.Should().Be(emailId);
    }

    [Fact]
    public async Task GetEmailAsync_UsesMetadataReceivedAtWhenResponseReceivedAtIsNull()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-without-received-at";
        var response = new EmailResponse
        {
            Id = emailId,
            InboxId = TestInboxHash,
            ReceivedAt = null, // ReceivedAt is null
            IsRead = false,
            EncryptedMetadata = CreateEncryptedPayload(),
            EncryptedParsed = CreateEncryptedPayload()
        };

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SetupDecryptionForMultipleEmails();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.ReceivedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task GetEmailAsync_HandlesNullEncryptedParsed()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-without-parsed";
        var response = new EmailResponse
        {
            Id = emailId,
            InboxId = TestInboxHash,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            EncryptedMetadata = CreateEncryptedPayload(),
            EncryptedParsed = null // No parsed content
        };

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SetupMetadataOnlyDecryption();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Text.Should().BeNull();
        email.Html.Should().BeNull();
        email.Attachments.Should().BeNull();
    }

    #endregion

    #region Metadata Fallback Tests

    [Fact]
    public async Task GetEmailsMetadataOnlyAsync_UsesResponseReceivedAtWhenAvailable()
    {
        // Arrange
        var inbox = CreateInbox();
        var expectedReceivedAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var response = new EmailResponse
        {
            Id = "email-1",
            InboxId = TestInboxHash,
            ReceivedAt = expectedReceivedAt,
            IsRead = true,
            EncryptedMetadata = CreateEncryptedPayload(),
            EncryptedParsed = null
        };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([response]);

        SetupMetadataOnlyDecryption();

        // Act
        var metadata = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadata.Should().HaveCount(1);
        metadata[0].ReceivedAt.Should().Be(expectedReceivedAt);
        metadata[0].IsRead.Should().BeTrue();
    }

    #endregion

    #region WaitForEmailAsync Additional Tests

    [Fact]
    public async Task WaitForEmailAsync_UsesDefaultOptionsWhenNull()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = new[] { CreateEmailResponse() };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        SetupDecryptionForMultipleEmails();

        // Act - pass null for options
        var email = await inbox.WaitForEmailAsync(null);

        // Assert
        email.Should().NotBeNull();
    }

    [Fact]
    public async Task WaitForEmailAsync_UsesFromFilter()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailResponses = new[] { CreateEmailResponse() };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        SetupDecryptionForMultipleEmails();

        // Act
        var options = new WaitForEmailOptions
        {
            From = "sender@example.com",
            Timeout = TimeSpan.FromMilliseconds(500)
        };
        var email = await inbox.WaitForEmailAsync(options);

        // Assert
        email.Should().NotBeNull();
        email.From.Should().Contain("sender@example.com");
    }

    #endregion

    #region Email with Metadata Tests

    [Fact]
    public async Task GetEmailAsync_ReturnsEmailWithMetadata()
    {
        // Arrange
        var inbox = CreateInbox();
        var emailId = "email-with-metadata";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse(emailId));

        SetupDecryptionWithMetadata();

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Metadata.Should().NotBeNull();
        email.Metadata!["customField"].ToString().Should().Be("customValue");
    }

    #endregion

    #region SyncWithServerAsync Tests

    [Fact]
    public async Task SyncWithServerAsync_SyncsNewEmailsWhenHashDiffers()
    {
        // Arrange - This test covers SyncWithServerAsync lines 377-446
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, onReconnected, _) =>
                {
                    capturedOnEmail = onEmail;
                    capturedOnReconnected = onReconnected;
                })
            .Returns(Task.CompletedTask);

        // Setup sync response - server has emails that local doesn't know about
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse
            {
                EmailCount = 2,
                EmailsHash = "server-hash-with-emails"
            });

        // Setup GetEmailsAsync to return email metadata (for sync discovery)
        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateEmailResponse("sync-email-1"),
                CreateEmailResponse("sync-email-2")
            ]);

        // Setup GetEmailAsync for fetching individual emails during sync
        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string emailId, CancellationToken _) => CreateEmailResponse(emailId));

        SetupDecryptionForMultipleEmails();

        // First, trigger a subscription via GetEmailsAsync
        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        await inbox.GetEmailsAsync();

        // Act - Invoke the sync callback (simulating reconnection)
        capturedOnReconnected.Should().NotBeNull();
        await capturedOnReconnected!();

        // Assert - Verify the sync fetched emails from server
        _mockApiClient.Verify(
            x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "sync-email-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "sync-email-2", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncWithServerAsync_SkipsSyncWhenHashMatches()
    {
        // Arrange - Test the case where local and server hash match (lines 389-393)
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, _, _, onReconnected, _) => capturedOnReconnected = onReconnected)
            .Returns(Task.CompletedTask);

        // Empty inbox - hash should be empty/base hash
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse
            {
                EmailCount = 0,
                EmailsHash = EmailHashCalculator.ComputeHash(Enumerable.Empty<string>())
            });

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        await inbox.GetEmailsAsync();

        // Act
        capturedOnReconnected.Should().NotBeNull();
        await capturedOnReconnected!();

        // Assert - Should not call GetEmailsAsync with includeContent=false since hashes match
        _mockApiClient.Verify(
            x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SyncWithServerAsync_RemovesDeletedEmails()
    {
        // Arrange - Test the case where local has emails that server doesn't (lines 406-417)
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, onReconnected, _) =>
                {
                    capturedOnEmail = onEmail;
                    capturedOnReconnected = onReconnected;
                })
            .Returns(Task.CompletedTask);

        // First setup - return 2 emails initially
        var initialEmails = new[] { CreateEmailResponse("email-1"), CreateEmailResponse("email-2") };
        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialEmails);

        SetupDecryptionForMultipleEmails();

        // Trigger initial load
        await inbox.GetEmailsAsync();

        // Setup for sync - server now only has email-1 (email-2 was deleted)
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse
            {
                EmailCount = 1,
                EmailsHash = "different-hash"
            });

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateEmailResponse("email-1")]);

        // Act - Trigger sync
        capturedOnReconnected.Should().NotBeNull();
        await capturedOnReconnected!();

        // Assert - Verify sync was called
        _mockApiClient.Verify(
            x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncWithServerAsync_HandlesExceptionGracefully()
    {
        // Arrange - Test error handling in sync (lines 442-445)
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, _, _, onReconnected, _) => capturedOnReconnected = onReconnected)
            .Returns(Task.CompletedTask);

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        await inbox.GetEmailsAsync();

        // Act - Sync should not throw, just log the error
        capturedOnReconnected.Should().NotBeNull();
        var syncAction = () => capturedOnReconnected!();
        await syncAction.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncWithServerAsync_FetchesNewEmailsOnSync()
    {
        // Arrange - Test fetching new emails during sync (lines 419-438)
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;
        var emailsWrittenToChannel = new List<string>();

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, _, _, onReconnected, _) => capturedOnReconnected = onReconnected)
            .Returns(Task.CompletedTask);

        // Initially empty
        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        SetupDecryptionForMultipleEmails();

        await inbox.GetEmailsAsync();

        // Setup sync to find new emails
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse
            {
                EmailCount = 2,
                EmailsHash = "hash-with-new-emails"
            });

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateEmailResponse("new-email-1"), CreateEmailResponse("new-email-2")]);

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string emailId, CancellationToken _) => CreateEmailResponse(emailId));

        // Act
        capturedOnReconnected.Should().NotBeNull();
        await capturedOnReconnected!();

        // Assert - Both new emails should have been fetched
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "new-email-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "new-email-2", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncWithServerAsync_HandlesEmailFetchFailure()
    {
        // Arrange - Test error handling when fetching individual email fails (lines 432-437)
        var inbox = CreateInbox();
        Func<Task>? capturedOnReconnected = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, _, _, onReconnected, _) => capturedOnReconnected = onReconnected)
            .Returns(Task.CompletedTask);

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        SetupDecryptionForMultipleEmails();

        await inbox.GetEmailsAsync();

        // Setup sync
        _mockApiClient
            .Setup(x => x.GetInboxSyncAsync(TestEmailAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse
            {
                EmailCount = 1,
                EmailsHash = "hash-with-email"
            });

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateEmailResponse("failing-email")]);

        // Make GetEmailAsync fail
        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, "failing-email", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failed to fetch email"));

        // Act - Should not throw
        capturedOnReconnected.Should().NotBeNull();
        var syncAction = () => capturedOnReconnected!();
        await syncAction.Should().NotThrowAsync();
    }

    #endregion

    #region OnEmailReceivedAsync Tests

    [Fact]
    public async Task OnEmailReceivedAsync_SkipsDuplicateEmails()
    {
        // Arrange - Test duplicate email handling (lines 356-361)
        var inbox = CreateInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, "dup-email-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmailResponse("dup-email-1"));

        SetupDecryptionForMultipleEmails();

        await inbox.GetEmailsAsync();

        // Act - Send the same email event twice
        capturedOnEmail.Should().NotBeNull();
        var emailEvent = new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "dup-email-1",
            EncryptedMetadata = CreateEncryptedPayload()
        };

        await capturedOnEmail!(emailEvent);
        await capturedOnEmail(emailEvent); // Duplicate

        // Assert - GetEmailAsync should only be called once
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "dup-email-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnEmailReceivedAsync_HandlesExceptionGracefully()
    {
        // Arrange - Test error handling (lines 371-374)
        var inbox = CreateInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, "error-email", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        await inbox.GetEmailsAsync();

        // Act - Should not throw
        capturedOnEmail.Should().NotBeNull();
        var emailEvent = new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "error-email",
            EncryptedMetadata = CreateEncryptedPayload()
        };

        var action = () => capturedOnEmail!(emailEvent);
        await action.Should().NotThrowAsync();
    }

    #endregion

    #region Helper Methods

    private void SetupDecryptionForMultipleEmails()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            Html = "<p>Test body HTML</p>"
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // Alternates between metadata and parsed
                return callCount++ % 2 == 0 ? metadataBytes : parsedBytes;
            });
    }

    private void SetupMetadataOnlyDecryption()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);

        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadataBytes);
    }

    private void SetupDecryptionWithAttachments()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            Html = "<p>Test body HTML</p>",
            Attachments =
            [
                new AttachmentData
                {
                    Filename = "test.txt",
                    ContentType = "text/plain",
                    Size = 11,
                    ContentId = "content-123",
                    ContentDisposition = "attachment",
                    Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello World")),
                    Checksum = "abc123"
                }
            ]
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    private void SetupDecryptionWithHeaders()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        // Create headers with various JSON types
        var headersJson = """
        {
            "text": "Test body text",
            "headers": {
                "X-Custom-Header": "custom-value",
                "X-Priority": 1,
                "X-Boolean-True": true,
                "X-Boolean-False": false,
                "X-Null-Header": null
            }
        }
        """;

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = Encoding.UTF8.GetBytes(headersJson);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    private void SetupDecryptionWithComplexHeaders()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        // Create headers with array and object types
        var headersJson = """
        {
            "text": "Test body text",
            "headers": {
                "X-Array-Header": ["item1", "item2"],
                "X-Object-Header": {"key": "value"},
                "X-Unknown-Type": {"complex": "structure"}
            }
        }
        """;

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = Encoding.UTF8.GetBytes(headersJson);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    private void SetupDecryptionWithAuthResults()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            AuthResults = new AuthenticationResults
            {
                Spf = new SpfResult { Result = SpfStatus.Pass, Domain = "example.com" },
                Dkim = [new DkimResult { Result = DkimStatus.Pass, Domain = "example.com" }],
                Dmarc = new DmarcResult { Result = DmarcStatus.Pass }
            }
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    private void SetupDecryptionWithLinks()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            Links = ["https://example.com", "https://test.com"]
        };

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    private void SetupDecryptionWithMetadata()
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        // Metadata is Dictionary<string, object> but needs custom JSON handling
        var parsedJson = """
        {
            "text": "Test body text",
            "metadata": {
                "customField": "customValue"
            }
        }
        """;

        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedBytes = Encoding.UTF8.GetBytes(parsedJson);

        var callCount = 0;
        _mockCryptoProvider
            .Setup(x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => callCount++ % 2 == 0 ? metadataBytes : parsedBytes);
    }

    #endregion

    #region Plain (Non-Encrypted) Inbox Tests

    private Inbox CreatePlainInbox(ILogger<Inbox>? logger = null)
    {
        return new Inbox(
            TestEmailAddress,
            DateTimeOffset.UtcNow.AddHours(1),
            TestInboxHash,
            emailAuth: true,
            _mockApiClient.Object,
            _mockDeliveryStrategy.Object,
            _options,
            logger);
    }

    private static EmailResponse CreatePlainEmailResponse(string id = "email-1")
    {
        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Test Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };

        var parsed = new DecryptedParsed
        {
            Text = "Test body text",
            Html = "<p>Test body HTML</p>"
        };

        var metadataJson = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);
        var parsedJson = JsonSerializer.SerializeToUtf8Bytes(parsed, VaultSandboxJsonContext.Default.DecryptedParsed);

        return new EmailResponse
        {
            Id = id,
            InboxId = TestInboxHash,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            Metadata = Convert.ToBase64String(metadataJson),
            Parsed = Convert.ToBase64String(parsedJson)
        };
    }

    [Fact]
    public void PlainInbox_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var inbox = new Inbox(
            TestEmailAddress,
            expiresAt,
            TestInboxHash,
            emailAuth: true,
            _mockApiClient.Object,
            _mockDeliveryStrategy.Object,
            _options);

        // Assert
        inbox.EmailAddress.Should().Be(TestEmailAddress);
        inbox.ExpiresAt.Should().Be(expiresAt);
        inbox.InboxHash.Should().Be(TestInboxHash);
        inbox.Encrypted.Should().BeFalse();
        inbox.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void EncryptedInbox_Constructor_SetsEncryptedTrue()
    {
        // Arrange & Act
        var inbox = CreateInbox();

        // Assert
        inbox.Encrypted.Should().BeTrue();
    }

    [Fact]
    public async Task PlainInbox_GetEmailsAsync_DecodesBase64Emails()
    {
        // Arrange
        var inbox = CreatePlainInbox();
        var emailResponses = new[] { CreatePlainEmailResponse("email-1"), CreatePlainEmailResponse("email-2") };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailResponses);

        // Act
        var emails = await inbox.GetEmailsAsync();

        // Assert
        emails.Should().HaveCount(2);
        emails[0].From.Should().Be("sender@example.com");
        emails[0].Subject.Should().Be("Test Subject");
        emails[0].Text.Should().Be("Test body text");
        emails[0].Html.Should().Be("<p>Test body HTML</p>");

        // Verify crypto provider was NOT called (plain emails don't need decryption)
        _mockCryptoProvider.Verify(
            x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlainInbox_GetEmailAsync_DecodesBase64Email()
    {
        // Arrange
        var inbox = CreatePlainInbox();
        var emailId = "test-email-id";

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlainEmailResponse(emailId));

        // Act
        var email = await inbox.GetEmailAsync(emailId);

        // Assert
        email.Id.Should().Be(emailId);
        email.From.Should().Be("sender@example.com");
        email.Subject.Should().Be("Test Subject");

        // Verify crypto provider was NOT called
        _mockCryptoProvider.Verify(
            x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlainInbox_GetEmailsMetadataOnlyAsync_DecodesBase64Metadata()
    {
        // Arrange
        var inbox = CreatePlainInbox();

        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "Metadata Only Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };
        var metadataJson = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);

        var response = new EmailResponse
        {
            Id = "email-1",
            InboxId = TestInboxHash,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            Metadata = Convert.ToBase64String(metadataJson),
            Parsed = null // No parsed content for metadata-only
        };

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([response]);

        // Act
        var metadataList = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadataList.Should().HaveCount(1);
        metadataList[0].From.Should().Be("sender@example.com");
        metadataList[0].Subject.Should().Be("Metadata Only Subject");
    }

    [Fact]
    public async Task PlainInbox_GetEmailRawAsync_DecodesBase64Raw()
    {
        // Arrange
        var inbox = CreatePlainInbox();
        var emailId = "test-email-id";
        var rawContent = "From: sender@example.com\r\nTo: test@example.com\r\nSubject: Test\r\n\r\nBody";

        var rawResponse = new RawEmailResponse
        {
            Id = emailId,
            Raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawContent))
        };

        _mockApiClient
            .Setup(x => x.GetRawEmailAsync(TestEmailAddress, emailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawResponse);

        // Act
        var raw = await inbox.GetEmailRawAsync(emailId);

        // Assert
        raw.Should().Be(rawContent);

        // Verify crypto provider was NOT called
        _mockCryptoProvider.Verify(
            x => x.DecryptAsync(
                It.IsAny<EncryptedPayload>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlainInbox_ExportAsync_ReturnsExportWithoutSecretKey()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var inbox = new Inbox(
            TestEmailAddress,
            expiresAt,
            TestInboxHash,
            emailAuth: true,
            _mockApiClient.Object,
            _mockDeliveryStrategy.Object,
            _options);

        // Act
        var export = await inbox.ExportAsync();

        // Assert
        export.Version.Should().Be(1);
        export.EmailAddress.Should().Be(TestEmailAddress);
        export.ExpiresAt.Should().Be(expiresAt);
        export.InboxHash.Should().Be(TestInboxHash);
        export.Encrypted.Should().BeFalse();
        export.ServerSigPk.Should().BeNull();
        export.SecretKey.Should().BeNull();
    }

    [Fact]
    public async Task EncryptedInbox_ExportAsync_ReturnsExportWithSecretKey()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var inbox = new Inbox(
            TestEmailAddress,
            expiresAt,
            TestInboxHash,
            emailAuth: true,
            TestServerSigPk,
            _keyPair,
            _mockApiClient.Object,
            _mockCryptoProvider.Object,
            _mockDeliveryStrategy.Object,
            _options);

        // Act
        var export = await inbox.ExportAsync();

        // Assert
        export.Encrypted.Should().BeTrue();
        export.ServerSigPk.Should().Be(TestServerSigPk);
        export.SecretKey.Should().NotBeNull();
    }

    [Fact]
    public async Task PlainInbox_OnEmailReceived_DecodesPlainSseEvent()
    {
        // Arrange
        var inbox = CreatePlainInbox();
        Func<SseEmailEvent, Task>? capturedOnEmail = null;

        _mockDeliveryStrategy
            .Setup(x => x.SubscribeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Func<SseEmailEvent, Task>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Func<SseEmailEvent, Task>, TimeSpan, Func<Task>?, CancellationToken>(
                (_, _, onEmail, _, _, _) => capturedOnEmail = onEmail)
            .Returns(Task.CompletedTask);

        _mockApiClient
            .Setup(x => x.GetEmailsAsync(TestEmailAddress, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailResponse>());

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, "plain-sse-email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePlainEmailResponse("plain-sse-email"));

        await inbox.GetEmailsAsync();

        // Act - Simulate plain SSE event
        capturedOnEmail.Should().NotBeNull();

        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "SSE Event Subject",
            ReceivedAt = DateTimeOffset.UtcNow
        };
        var metadataJson = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);

        var emailEvent = new SseEmailEvent
        {
            InboxId = TestInboxHash,
            EmailId = "plain-sse-email",
            Metadata = Convert.ToBase64String(metadataJson) // Plain format
        };

        await capturedOnEmail!(emailEvent);

        // Assert - Email was fetched and processed
        _mockApiClient.Verify(
            x => x.GetEmailAsync(TestEmailAddress, "plain-sse-email", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PlainInbox_HandlesNullParsed()
    {
        // Arrange
        var inbox = CreatePlainInbox();

        var metadata = new DecryptedMetadata
        {
            From = "sender@example.com",
            To = [TestEmailAddress],
            Subject = "No Parsed Content",
            ReceivedAt = DateTimeOffset.UtcNow
        };
        var metadataJson = JsonSerializer.SerializeToUtf8Bytes(metadata, VaultSandboxJsonContext.Default.DecryptedMetadata);

        var response = new EmailResponse
        {
            Id = "email-without-parsed",
            InboxId = TestInboxHash,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRead = false,
            Metadata = Convert.ToBase64String(metadataJson),
            Parsed = null // No parsed content
        };

        _mockApiClient
            .Setup(x => x.GetEmailAsync(TestEmailAddress, "email-without-parsed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var email = await inbox.GetEmailAsync("email-without-parsed");

        // Assert
        email.From.Should().Be("sender@example.com");
        email.Subject.Should().Be("No Parsed Content");
        email.Text.Should().BeNull();
        email.Html.Should().BeNull();
    }

    [Fact]
    public void EmailResponse_IsEncrypted_ReturnsTrueWhenEncryptedMetadataPresent()
    {
        // Arrange
        var encryptedResponse = CreateEmailResponse();

        // Act & Assert
        encryptedResponse.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void EmailResponse_IsEncrypted_ReturnsFalseWhenOnlyPlainMetadataPresent()
    {
        // Arrange
        var plainResponse = CreatePlainEmailResponse();

        // Act & Assert
        plainResponse.IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void RawEmailResponse_IsEncrypted_ReturnsTrueWhenEncryptedRawPresent()
    {
        // Arrange
        var response = new RawEmailResponse
        {
            Id = "test",
            EncryptedRaw = CreateEncryptedPayload()
        };

        // Act & Assert
        response.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void RawEmailResponse_IsEncrypted_ReturnsFalseWhenOnlyRawPresent()
    {
        // Arrange
        var response = new RawEmailResponse
        {
            Id = "test",
            Raw = "base64content"
        };

        // Act & Assert
        response.IsEncrypted.Should().BeFalse();
    }

    [Fact]
    public void SseEmailEvent_IsEncrypted_ReturnsTrueWhenEncryptedMetadataPresent()
    {
        // Arrange
        var evt = new SseEmailEvent
        {
            InboxId = "inbox",
            EmailId = "email",
            EncryptedMetadata = CreateEncryptedPayload()
        };

        // Act & Assert
        evt.IsEncrypted.Should().BeTrue();
    }

    [Fact]
    public void SseEmailEvent_IsEncrypted_ReturnsFalseWhenOnlyPlainMetadataPresent()
    {
        // Arrange
        var evt = new SseEmailEvent
        {
            InboxId = "inbox",
            EmailId = "email",
            Metadata = "base64metadata"
        };

        // Act & Assert
        evt.IsEncrypted.Should().BeFalse();
    }

    #endregion
}
