using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit;

public class VaultSandboxClientBuilderTests : IAsyncLifetime
{
    private readonly List<IDisposable> _syncDisposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var disposable in _asyncDisposables)
        {
            await disposable.DisposeAsync();
        }
        foreach (var disposable in _syncDisposables)
        {
            disposable.Dispose();
        }
    }

    #region Create Factory Method

    [Fact]
    public void Create_ShouldReturnNewBuilderInstance()
    {
        // Act
        var builder = VaultSandboxClientBuilder.Create();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<VaultSandboxClientBuilder>();
    }

    [Fact]
    public void Create_CalledMultipleTimes_ShouldReturnDifferentInstances()
    {
        // Act
        var builder1 = VaultSandboxClientBuilder.Create();
        var builder2 = VaultSandboxClientBuilder.Create();

        // Assert
        builder1.Should().NotBeSameAs(builder2);
    }

    #endregion

    #region Fluent API - Method Chaining

    [Fact]
    public void WithBaseUrl_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithBaseUrl("https://example.com");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithApiKey_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithApiKey("test-key");

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithHttpTimeout_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithHttpTimeout(TimeSpan.FromSeconds(60));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithWaitTimeout_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithWaitTimeout(TimeSpan.FromSeconds(60));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithPollInterval_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithPollInterval(TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMaxRetries_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithMaxRetries(5);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithRetryDelay_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithRetryDelay(TimeSpan.FromSeconds(2));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithSseReconnectInterval_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithSseReconnectInterval(TimeSpan.FromSeconds(5));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithSseMaxReconnectAttempts_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithSseMaxReconnectAttempts(20);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithDeliveryStrategy_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithDeliveryStrategy(DeliveryStrategy.Sse);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithDefaultInboxTtl_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.WithDefaultInboxTtl(TimeSpan.FromHours(2));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithLogging_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();
        var loggerFactory = new Mock<ILoggerFactory>().Object;

        // Act
        var result = builder.WithLogging(loggerFactory);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithHttpClient_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();
        var httpClient = new HttpClient();
        _syncDisposables.Add(httpClient);

        // Act
        var result = builder.WithHttpClient(httpClient);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AllWithMethods_ShouldSupportMethodChaining()
    {
        // Arrange & Act
        var loggerFactory = new Mock<ILoggerFactory>().Object;
        var httpClient = new HttpClient();
        _syncDisposables.Add(httpClient);

        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpTimeout(TimeSpan.FromSeconds(60))
            .WithWaitTimeout(TimeSpan.FromSeconds(120))
            .WithPollInterval(TimeSpan.FromSeconds(5))
            .WithMaxRetries(5)
            .WithRetryDelay(TimeSpan.FromSeconds(2))
            .WithSseReconnectInterval(TimeSpan.FromSeconds(3))
            .WithSseMaxReconnectAttempts(15)
            .WithDeliveryStrategy(DeliveryStrategy.Polling)
            .WithDefaultInboxTtl(TimeSpan.FromHours(2))
            .WithLogging(loggerFactory)
            .WithHttpClient(httpClient);

        // Assert
        builder.Should().NotBeNull();
    }

    #endregion

    #region Delivery Strategy Convenience Methods

    [Fact]
    public void UseSseDelivery_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.UseSseDelivery();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void UsePollingDelivery_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create();

        // Act
        var result = builder.UsePollingDelivery();

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region Build - Required Fields Validation

    [Fact]
    public void Build_WithoutBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithApiKey("test-key");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl*required*");
    }

    [Fact]
    public void Build_WithoutApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiKey*required*");
    }

    [Fact]
    public void Build_WithEmptyBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("")
            .WithApiKey("test-key");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    [Fact]
    public void Build_WithEmptyApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ApiKey*");
    }

    [Fact]
    public void Build_WithInvalidBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("not-a-valid-url")
            .WithApiKey("test-key");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    [Fact]
    public void Build_WithFtpBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("ftp://example.com")
            .WithApiKey("test-key");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*http*https*");
    }

    #endregion

    #region Build - Valid Configuration

    [Fact]
    public void Build_WithMinimalValidConfig_ShouldReturnClient()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key");

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IVaultSandboxClient>();
    }

    [Fact]
    public void Build_WithHttpBaseUrl_ShouldReturnClient()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("http://localhost:3000")
            .WithApiKey("test-key");

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithFullValidConfig_ShouldReturnClient()
    {
        // Arrange
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpTimeout(TimeSpan.FromSeconds(60))
            .WithWaitTimeout(TimeSpan.FromSeconds(120))
            .WithPollInterval(TimeSpan.FromSeconds(5))
            .WithMaxRetries(5)
            .WithRetryDelay(TimeSpan.FromSeconds(2))
            .WithSseReconnectInterval(TimeSpan.FromSeconds(3))
            .WithSseMaxReconnectAttempts(15)
            .WithDeliveryStrategy(DeliveryStrategy.Polling)
            .WithDefaultInboxTtl(TimeSpan.FromHours(2))
            .WithLogging(loggerFactory.Object);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    #endregion

    #region Build - Invalid Option Values

    [Fact]
    public void Build_WithZeroHttpTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpTimeout(TimeSpan.Zero);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HttpTimeoutMs*positive*");
    }

    [Fact]
    public void Build_WithNegativeHttpTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpTimeout(TimeSpan.FromSeconds(-1));

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*HttpTimeoutMs*positive*");
    }

    [Fact]
    public void Build_WithZeroWaitTimeout_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithWaitTimeout(TimeSpan.Zero);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*WaitTimeoutMs*positive*");
    }

    [Fact]
    public void Build_WithZeroPollInterval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithPollInterval(TimeSpan.Zero);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PollIntervalMs*positive*");
    }

    [Fact]
    public void Build_WithNegativeMaxRetries_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithMaxRetries(-1);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MaxRetries*negative*");
    }

    [Fact]
    public void Build_WithZeroMaxRetries_ShouldSucceed()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithMaxRetries(0);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithZeroRetryDelay_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithRetryDelay(TimeSpan.Zero);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RetryDelayMs*positive*");
    }

    [Fact]
    public void Build_WithZeroSseReconnectInterval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithSseReconnectInterval(TimeSpan.Zero);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SseReconnectIntervalMs*positive*");
    }

    [Fact]
    public void Build_WithNegativeSseMaxReconnectAttempts_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithSseMaxReconnectAttempts(-1);

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SseMaxReconnectAttempts*negative*");
    }

    [Fact]
    public void Build_WithZeroSseMaxReconnectAttempts_ShouldSucceed()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithSseMaxReconnectAttempts(0);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithTooSmallInboxTtl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithDefaultInboxTtl(TimeSpan.FromSeconds(59));

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultInboxTtlSeconds*60*");
    }

    [Fact]
    public void Build_WithMinimumInboxTtl_ShouldSucceed()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithDefaultInboxTtl(TimeSpan.FromSeconds(60));

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    #endregion

    #region Build - HttpClient Handling

    [Fact]
    public void Build_WithCustomHttpClient_ShouldUseProvidedClient()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://example.com")
        };
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-key");
        _syncDisposables.Add(httpClient);

        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpClient(httpClient);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithCustomHttpClientAndDisposeTrue_ShouldCreateClient()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://example.com")
        };
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-key");

        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpClient(httpClient, disposeClient: true);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithCustomHttpClientAndDisposeFalse_ShouldCreateClient()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://example.com")
        };
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-key");
        _syncDisposables.Add(httpClient);

        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithHttpClient(httpClient, disposeClient: false);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    #endregion

    #region Build - Multiple Builds

    [Fact]
    public void Build_CalledMultipleTimes_ShouldReturnDifferentClients()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key");

        // Act
        var client1 = builder.Build();
        var client2 = builder.Build();
        _asyncDisposables.Add(client1);
        _asyncDisposables.Add(client2);

        // Assert
        client1.Should().NotBeSameAs(client2);
    }

    #endregion

    #region BuildAndValidateAsync

    [Fact]
    public async Task BuildAndValidateAsync_WithoutBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithApiKey("test-key");

        // Act
        var act = async () => await builder.BuildAndValidateAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BaseUrl*required*");
    }

    [Fact]
    public async Task BuildAndValidateAsync_WithoutApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com");

        // Act
        var act = async () => await builder.BuildAndValidateAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ApiKey*required*");
    }

    #endregion

    #region DeliveryStrategy Options

    [Fact]
    public void Build_WithSseDeliveryStrategy_ShouldSucceed()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .UseSseDelivery();

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithPollingDeliveryStrategy_ShouldSucceed()
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .UsePollingDelivery();

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    [Theory]
    [InlineData(DeliveryStrategy.Sse)]
    [InlineData(DeliveryStrategy.Polling)]
    public void Build_WithAllDeliveryStrategies_ShouldSucceed(DeliveryStrategy strategy)
    {
        // Arrange
        var builder = VaultSandboxClientBuilder.Create()
            .WithBaseUrl("https://example.com")
            .WithApiKey("test-key")
            .WithDeliveryStrategy(strategy);

        // Act
        var client = builder.Build();
        _asyncDisposables.Add(client);

        // Assert
        client.Should().NotBeNull();
    }

    #endregion
}
