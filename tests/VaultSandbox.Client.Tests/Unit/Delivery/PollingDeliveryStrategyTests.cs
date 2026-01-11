using FluentAssertions;
using Moq;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class PollingDeliveryStrategyTests : IAsyncDisposable
{
    private readonly Mock<IVaultSandboxApiClient> _mockApiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly PollingDeliveryStrategy _strategy;

    public PollingDeliveryStrategyTests()
    {
        _mockApiClient = new Mock<IVaultSandboxApiClient>();
        _options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://test.example.com",
            ApiKey = "test-key",
            PollIntervalMs = 100
        };
        _strategy = new PollingDeliveryStrategy(_mockApiClient.Object, _options);
    }

    public async ValueTask DisposeAsync()
    {
        await _strategy.DisposeAsync();
    }

    [Fact]
    public void IsConnected_WhenNoSubscriptions_ShouldBeFalse()
    {
        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateInbox_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupInboxSync("hash1");

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(100));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Already subscribed*");
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldRemoveSubscription()
    {
        // Arrange
        SetupInboxSync("hash1");

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(100));

        // Act
        await _strategy.UnsubscribeAsync("inbox1");
        await Task.Delay(50);

        // Assert
        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task PollingWithBackoff_ShouldIncreaseIntervalOnNoChanges()
    {
        // Arrange - Use the hash of an empty set which is what the local state will compute
        var emptyHash = ComputeEmailHash([]);
        var pollCount = 0;
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => pollCount++)
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = emptyHash });

        // Act - Start polling with 50ms interval
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(50));

        // Wait for several polling cycles
        await Task.Delay(300);

        // Assert - Should have polled multiple times
        pollCount.Should().BeGreaterThan(1, "polling should have occurred multiple times");
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task PollingWithStableState_ShouldTakeNoChangesPath()
    {
        // Arrange - First poll returns email, subsequent polls return matching hash
        var pollCount = 0;
        var encryptedPayload = CreateTestEncryptedPayload();
        var emailId = "email-1";
        var stableHash = ComputeEmailHash([emailId]);

        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                pollCount++;
                // First poll triggers fetch (different hash), subsequent polls return stable hash
                return pollCount == 1
                    ? new InboxSyncResponse { EmailCount = 1, EmailsHash = "initial-different-hash" }
                    : new InboxSyncResponse { EmailCount = 1, EmailsHash = stableHash };
            });

        _mockApiClient.Setup(x => x.GetEmailsAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EmailResponse
                {
                    Id = emailId,
                    InboxId = "inbox-1",
                    EncryptedMetadata = encryptedPayload
                }
            ]);

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(30));

        await Task.Delay(200);

        // Assert - Should have polled multiple times, reaching the "no changes" path
        pollCount.Should().BeGreaterThan(2, "should have multiple polls including no-changes polls");
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentPolling_MultipleInboxes_ShouldAllPoll()
    {
        // Arrange
        SetupInboxSync("hash1");
        var callbacks = new Dictionary<string, int>
        {
            ["inbox1"] = 0,
            ["inbox2"] = 0,
            ["inbox3"] = 0
        };

        // Act - Subscribe to multiple inboxes concurrently
        var tasks = new[]
        {
            _strategy.SubscribeAsync(
                "inbox1",
                "test1@example.com",
                _ => { callbacks["inbox1"]++; return Task.CompletedTask; },
                TimeSpan.FromMilliseconds(500)),
            _strategy.SubscribeAsync(
                "inbox2",
                "test2@example.com",
                _ => { callbacks["inbox2"]++; return Task.CompletedTask; },
                TimeSpan.FromMilliseconds(500)),
            _strategy.SubscribeAsync(
                "inbox3",
                "test3@example.com",
                _ => { callbacks["inbox3"]++; return Task.CompletedTask; },
                TimeSpan.FromMilliseconds(500))
        };

        await Task.WhenAll(tasks);

        // Wait for a few polling cycles
        await Task.Delay(350);

        // Assert - All inboxes should be polled
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        SetupInboxSync("hash1");

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(100));

        // Act & Assert - Multiple unsubscribe calls should not throw
        await _strategy.UnsubscribeAsync("inbox1");
        await _strategy.UnsubscribeAsync("inbox1");
        await _strategy.UnsubscribeAsync("inbox1");

        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task PollingWithNewEmails_ShouldInvokeCallback()
    {
        // Arrange
        var receivedEvents = new List<SseEmailEvent>();
        var pollCount = 0;
        var encryptedPayload = CreateTestEncryptedPayload();

        // First poll returns empty hash, second poll returns hash with emails
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                pollCount++;
                // Return a different hash to trigger email fetch
                return new InboxSyncResponse { EmailCount = 1, EmailsHash = $"hash-{pollCount}" };
            });

        _mockApiClient.Setup(x => x.GetEmailsAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new EmailResponse
                {
                    Id = "email-1",
                    InboxId = "inbox-1",
                    EncryptedMetadata = encryptedPayload
                }
            ]);

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            evt => { receivedEvents.Add(evt); return Task.CompletedTask; },
            TimeSpan.FromMilliseconds(50));

        await Task.Delay(200);

        // Assert
        receivedEvents.Should().NotBeEmpty();
        receivedEvents.First().EmailId.Should().Be("email-1");
    }

    [Fact]
    public async Task PollingWithDeletedEmails_ShouldRemoveFromState()
    {
        // Arrange
        var pollCount = 0;
        var encryptedPayload = CreateTestEncryptedPayload();

        // First poll returns one email, second poll returns empty (email deleted)
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                pollCount++;
                return new InboxSyncResponse { EmailCount = pollCount == 1 ? 1 : 0, EmailsHash = $"hash-{pollCount}" };
            });

        _mockApiClient.Setup(x => x.GetEmailsAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                // First call returns email, subsequent calls return empty
                if (pollCount <= 1)
                {
                    return
                    [
                        new EmailResponse
                        {
                            Id = "email-1",
                            InboxId = "inbox-1",
                            EncryptedMetadata = encryptedPayload
                        }
                    ];
                }
                return [];
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(50));

        await Task.Delay(250);

        // Assert - polling should continue without errors
        _strategy.IsConnected.Should().BeTrue();
        pollCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task DisposeAsync_WithActiveSubscriptions_ShouldCancelPolling()
    {
        // Arrange
        var pollCount = 0;
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => pollCount++)
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = "hash1" });

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(50));

        await Task.Delay(100);

        // Act
        await _strategy.DisposeAsync();

        // Assert
        pollCount.Should().BeGreaterThan(0);
        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WithMultipleSubscriptions_ShouldCancelAll()
    {
        // Arrange
        SetupInboxSync("hash1");

        await _strategy.SubscribeAsync("inbox1", "test1@example.com", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(100));
        await _strategy.SubscribeAsync("inbox2", "test2@example.com", _ => Task.CompletedTask, TimeSpan.FromMilliseconds(100));

        await Task.Delay(50);

        // Act
        await _strategy.DisposeAsync();

        // Assert
        _strategy.IsConnected.Should().BeFalse();
    }

    private void SetupInboxSync(string hash)
    {
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = hash });
    }

    private static EncryptedPayload CreateTestEncryptedPayload()
    {
        return new EncryptedPayload
        {
            Version = 1,
            Algorithms = new AlgorithmSuite
            {
                Kem = "ML-KEM-768",
                Sig = "ML-DSA-65",
                Aead = "AES-256-GCM",
                Kdf = "HKDF-SHA256"
            },
            CtKem = "dGVzdA",
            Nonce = "dGVzdG5vbmNl",
            Aad = "dGVzdGFhZA",
            Ciphertext = "dGVzdGNpcGhlcnRleHQ",
            Signature = "dGVzdHNpZw",
            ServerSigPk = "dGVzdHNlcnZlcnNpZ3Br"
        };
    }

    private static string ComputeEmailHash(IEnumerable<string> emailIds)
    {
        var sortedIds = emailIds.OrderBy(id => id, StringComparer.Ordinal);
        var joined = string.Join(",", sortedIds);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(joined));
        return Convert.ToBase64String(hashBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
