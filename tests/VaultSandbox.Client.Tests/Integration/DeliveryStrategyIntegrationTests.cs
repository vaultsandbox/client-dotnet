using FluentAssertions;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for delivery strategies against a real server.
/// Tests require a configured .env file with valid credentials.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class DeliveryStrategyIntegrationTests : IAsyncLifetime
{
    private readonly TestSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly VaultSandboxApiClient _apiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly CryptoProvider _cryptoProvider;
    private readonly List<string> _createdInboxes = [];

    public DeliveryStrategyIntegrationTests()
    {
        _settings = TestConfiguration.Settings;
        _cryptoProvider = new CryptoProvider();

        var handler = new ApiKeyAuthenticationHandler(_settings.ApiKey)
        {
            InnerHandler = new HttpClientHandler()
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_settings.BaseUrl)
        };

        _apiClient = new VaultSandboxApiClient(_httpClient);

        _options = new VaultSandboxClientOptions
        {
            BaseUrl = _settings.BaseUrl,
            ApiKey = _settings.ApiKey,
            PollIntervalMs = 500,
            SseReconnectIntervalMs = 1000,
            SseMaxReconnectAttempts = 3
        };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var email in _createdInboxes)
        {
            try
            {
                await _apiClient.DeleteInboxAsync(email);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _apiClient.Dispose();
        _httpClient.Dispose();
    }

    [SkippableFact]
    public async Task SseStrategy_ShouldConnectToServer()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var inbox = await CreateTestInboxAsync();
        await using var strategy = new SseDeliveryStrategy(_apiClient, _options);

        // Act
        await strategy.SubscribeAsync(
            inbox.InboxHash,
            inbox.EmailAddress,
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Wait for connection
        await Task.Delay(500);

        // Assert
        strategy.IsConnected.Should().BeTrue();
    }

    [SkippableFact]
    public async Task SseStrategy_ShouldDisconnectOnUnsubscribe()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var inbox = await CreateTestInboxAsync();
        await using var strategy = new SseDeliveryStrategy(_apiClient, _options);

        await strategy.SubscribeAsync(
            inbox.InboxHash,
            inbox.EmailAddress,
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(500);
        strategy.IsConnected.Should().BeTrue();

        // Act
        await strategy.UnsubscribeAsync(inbox.InboxHash);
        await Task.Delay(200);

        // Assert
        strategy.IsConnected.Should().BeFalse();
    }

    [SkippableFact]
    public async Task PollingStrategy_ShouldPollInbox()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var inbox = await CreateTestInboxAsync();
        await using var strategy = new PollingDeliveryStrategy(_apiClient, _options);
        var pollCount = 0;

        // Act
        await strategy.SubscribeAsync(
            inbox.InboxHash,
            inbox.EmailAddress,
            _ =>
            {
                pollCount++;
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(200));

        // Wait for a few polling cycles
        await Task.Delay(1000);

        // Assert
        strategy.IsConnected.Should().BeTrue();
    }

    [SkippableFact]
    public async Task PollingStrategy_ShouldStopOnUnsubscribe()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var inbox = await CreateTestInboxAsync();
        await using var strategy = new PollingDeliveryStrategy(_apiClient, _options);

        await strategy.SubscribeAsync(
            inbox.InboxHash,
            inbox.EmailAddress,
            _ => Task.CompletedTask,
            TimeSpan.FromMilliseconds(200));

        await Task.Delay(500);
        strategy.IsConnected.Should().BeTrue();

        // Act
        await strategy.UnsubscribeAsync(inbox.InboxHash);
        await Task.Delay(100);

        // Assert
        strategy.IsConnected.Should().BeFalse();
    }

    [SkippableFact]
    public async Task DeliveryStrategyFactory_ShouldCreateAllStrategies()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var factory = new DeliveryStrategyFactory(_apiClient, _options);

        // Act & Assert - SSE
        await using var sseStrategy = factory.Create(DeliveryStrategy.Sse);
        sseStrategy.Should().BeOfType<SseDeliveryStrategy>();

        // Act & Assert - Polling
        await using var pollingStrategy = factory.Create(DeliveryStrategy.Polling);
        pollingStrategy.Should().BeOfType<PollingDeliveryStrategy>();
    }

    private async Task<CreateInboxResponse> CreateTestInboxAsync()
    {
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var request = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };

        var inbox = await _apiClient.CreateInboxAsync(request);
        _createdInboxes.Add(inbox.EmailAddress);
        return inbox;
    }
}
