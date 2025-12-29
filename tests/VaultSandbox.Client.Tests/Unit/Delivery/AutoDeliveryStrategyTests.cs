using FluentAssertions;
using Moq;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class AutoDeliveryStrategyTests : IAsyncDisposable
{
    private readonly Mock<IVaultSandboxApiClient> _mockApiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly AutoDeliveryStrategy _strategy;

    public AutoDeliveryStrategyTests()
    {
        _mockApiClient = new Mock<IVaultSandboxApiClient>();
        _options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://test.example.com",
            ApiKey = "test-key",
            SseReconnectIntervalMs = 100,
            SseMaxReconnectAttempts = 2,
            PollIntervalMs = 100
        };
        _strategy = new AutoDeliveryStrategy(_mockApiClient.Object, _options);
    }

    public async ValueTask DisposeAsync()
    {
        await _strategy.DisposeAsync();
    }

    [Fact]
    public void IsConnected_InitialState_ShouldBeFalse()
    {
        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_WhenSseFails_ShouldFallbackToPolling()
    {
        // Arrange - SSE fails, polling succeeds
        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SSE not available"));

        _mockApiClient.Setup(x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InboxSyncResponse { EmailCount = 0, EmailsHash = "hash1" });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(100));

        // Wait for polling to start
        await Task.Delay(200);

        // Assert - polling should have been called (fallback occurred)
        _mockApiClient.Verify(
            x => x.GetInboxSyncAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
