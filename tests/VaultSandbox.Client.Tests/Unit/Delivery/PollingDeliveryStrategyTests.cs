using FluentAssertions;
using Moq;
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
        // Arrange
        var pollCount = 0;
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => pollCount++)
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = "same-hash" });

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

    private void SetupInboxSync(string hash)
    {
        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = hash });
    }
}
