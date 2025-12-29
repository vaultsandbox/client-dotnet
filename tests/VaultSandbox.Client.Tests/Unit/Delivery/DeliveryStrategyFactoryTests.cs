using FluentAssertions;
using Moq;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Http;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class DeliveryStrategyFactoryTests
{
    private readonly Mock<IVaultSandboxApiClient> _mockApiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly DeliveryStrategyFactory _factory;

    public DeliveryStrategyFactoryTests()
    {
        _mockApiClient = new Mock<IVaultSandboxApiClient>();
        _options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://test.example.com",
            ApiKey = "test-key"
        };
        _factory = new DeliveryStrategyFactory(_mockApiClient.Object, _options);
    }

    [Fact]
    public async Task Create_Sse_ShouldReturnSseDeliveryStrategy()
    {
        // Act
        var strategy = _factory.Create(DeliveryStrategy.Sse);

        // Assert
        strategy.Should().BeOfType<SseDeliveryStrategy>();
        await strategy.DisposeAsync();
    }

    [Fact]
    public async Task Create_Polling_ShouldReturnPollingDeliveryStrategy()
    {
        // Act
        var strategy = _factory.Create(DeliveryStrategy.Polling);

        // Assert
        strategy.Should().BeOfType<PollingDeliveryStrategy>();
        await strategy.DisposeAsync();
    }

    [Fact]
    public async Task Create_Auto_ShouldReturnAutoDeliveryStrategy()
    {
        // Act
        var strategy = _factory.Create(DeliveryStrategy.Auto);

        // Assert
        strategy.Should().BeOfType<AutoDeliveryStrategy>();
        await strategy.DisposeAsync();
    }

    [Fact]
    public void Create_InvalidStrategy_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        Action act = () => _factory.Create((DeliveryStrategy)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Create_MultipleStrategies_ShouldReturnDistinctInstances()
    {
        // Act
        var strategy1 = _factory.Create(DeliveryStrategy.Sse);
        var strategy2 = _factory.Create(DeliveryStrategy.Sse);

        // Assert
        strategy1.Should().NotBeSameAs(strategy2);

        await strategy1.DisposeAsync();
        await strategy2.DisposeAsync();
    }
}
