using FluentAssertions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit;

public class VaultSandboxClientOptionsTests
{
    #region Default Values

    [Fact]
    public void Constructor_ShouldSetDefaultHttpTimeoutMs()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.HttpTimeoutMs.Should().Be(30_000);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultWaitTimeoutMs()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.WaitTimeoutMs.Should().Be(30_000);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultPollIntervalMs()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.PollIntervalMs.Should().Be(2_000);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultMaxRetries()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.MaxRetries.Should().Be(3);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultRetryDelayMs()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.RetryDelayMs.Should().Be(1_000);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultSseReconnectIntervalMs()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.SseReconnectIntervalMs.Should().Be(2_000);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultSseMaxReconnectAttempts()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.SseMaxReconnectAttempts.Should().Be(10);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultDeliveryStrategy()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.DefaultDeliveryStrategy.Should().Be(DeliveryStrategy.Sse);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultInboxTtlSeconds()
    {
        // Arrange & Act
        var options = CreateValidOptions();

        // Assert
        options.DefaultInboxTtlSeconds.Should().Be(3600);
    }

    #endregion

    #region Property Setters/Getters

    [Fact]
    public void BaseUrl_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = "https://custom.example.com";

        // Act
        options.BaseUrl = expected;

        // Assert
        options.BaseUrl.Should().Be(expected);
    }

    [Fact]
    public void ApiKey_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = "custom-api-key-12345";

        // Act
        options.ApiKey = expected;

        // Assert
        options.ApiKey.Should().Be(expected);
    }

    [Fact]
    public void HttpTimeoutMs_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 60_000;

        // Act
        options.HttpTimeoutMs = expected;

        // Assert
        options.HttpTimeoutMs.Should().Be(expected);
    }

    [Fact]
    public void WaitTimeoutMs_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 120_000;

        // Act
        options.WaitTimeoutMs = expected;

        // Assert
        options.WaitTimeoutMs.Should().Be(expected);
    }

    [Fact]
    public void PollIntervalMs_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 5_000;

        // Act
        options.PollIntervalMs = expected;

        // Assert
        options.PollIntervalMs.Should().Be(expected);
    }

    [Fact]
    public void MaxRetries_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 10;

        // Act
        options.MaxRetries = expected;

        // Assert
        options.MaxRetries.Should().Be(expected);
    }

    [Fact]
    public void RetryDelayMs_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 2_000;

        // Act
        options.RetryDelayMs = expected;

        // Assert
        options.RetryDelayMs.Should().Be(expected);
    }

    [Fact]
    public void SseReconnectIntervalMs_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 5_000;

        // Act
        options.SseReconnectIntervalMs = expected;

        // Assert
        options.SseReconnectIntervalMs.Should().Be(expected);
    }

    [Fact]
    public void SseMaxReconnectAttempts_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 20;

        // Act
        options.SseMaxReconnectAttempts = expected;

        // Assert
        options.SseMaxReconnectAttempts.Should().Be(expected);
    }

    [Fact]
    public void DefaultDeliveryStrategy_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = DeliveryStrategy.Polling;

        // Act
        options.DefaultDeliveryStrategy = expected;

        // Assert
        options.DefaultDeliveryStrategy.Should().Be(expected);
    }

    [Fact]
    public void DefaultInboxTtlSeconds_SetValue_ShouldReturnSameValue()
    {
        // Arrange
        var options = CreateValidOptions();
        var expected = 7200;

        // Act
        options.DefaultInboxTtlSeconds = expected;

        // Assert
        options.DefaultInboxTtlSeconds.Should().Be(expected);
    }

    #endregion

    #region Validate - Valid Options

    [Fact]
    public void Validate_ValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidHttpBaseUrl_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "http://localhost:3000";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ValidHttpsBaseUrl_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "https://api.example.com";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validate - BaseUrl Validation

    [Fact]
    public void Validate_EmptyBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("BaseUrl is required");
    }

    [Fact]
    public void Validate_WhitespaceBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "   ";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("BaseUrl is required");
    }

    [Fact]
    public void Validate_InvalidBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "not-a-valid-url";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid absolute URI*");
    }

    [Fact]
    public void Validate_RelativeBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "/api/v1";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*http or https scheme*");
    }

    [Fact]
    public void Validate_FtpBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "ftp://example.com";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*http or https scheme*");
    }

    [Fact]
    public void Validate_FileBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "file:///path/to/file";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*http or https scheme*");
    }

    #endregion

    #region Validate - ApiKey Validation

    [Fact]
    public void Validate_EmptyApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApiKey is required");
    }

    [Fact]
    public void Validate_WhitespaceApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "   ";

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApiKey is required");
    }

    #endregion

    #region Validate - HttpTimeoutMs Validation

    [Fact]
    public void Validate_ZeroHttpTimeoutMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HttpTimeoutMs = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("HttpTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_NegativeHttpTimeoutMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HttpTimeoutMs = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("HttpTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_PositiveHttpTimeoutMs_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HttpTimeoutMs = 1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validate - WaitTimeoutMs Validation

    [Fact]
    public void Validate_ZeroWaitTimeoutMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.WaitTimeoutMs = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("WaitTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_NegativeWaitTimeoutMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.WaitTimeoutMs = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("WaitTimeoutMs must be positive");
    }

    #endregion

    #region Validate - PollIntervalMs Validation

    [Fact]
    public void Validate_ZeroPollIntervalMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.PollIntervalMs = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("PollIntervalMs must be positive");
    }

    [Fact]
    public void Validate_NegativePollIntervalMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.PollIntervalMs = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("PollIntervalMs must be positive");
    }

    #endregion

    #region Validate - MaxRetries Validation

    [Fact]
    public void Validate_NegativeMaxRetries_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRetries = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MaxRetries cannot be negative");
    }

    [Fact]
    public void Validate_ZeroMaxRetries_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRetries = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validate - RetryDelayMs Validation

    [Fact]
    public void Validate_ZeroRetryDelayMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.RetryDelayMs = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("RetryDelayMs must be positive");
    }

    [Fact]
    public void Validate_NegativeRetryDelayMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.RetryDelayMs = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("RetryDelayMs must be positive");
    }

    #endregion

    #region Validate - SseReconnectIntervalMs Validation

    [Fact]
    public void Validate_ZeroSseReconnectIntervalMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseReconnectIntervalMs = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SseReconnectIntervalMs must be positive");
    }

    [Fact]
    public void Validate_NegativeSseReconnectIntervalMs_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseReconnectIntervalMs = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SseReconnectIntervalMs must be positive");
    }

    #endregion

    #region Validate - SseMaxReconnectAttempts Validation

    [Fact]
    public void Validate_NegativeSseMaxReconnectAttempts_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseMaxReconnectAttempts = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("SseMaxReconnectAttempts cannot be negative");
    }

    [Fact]
    public void Validate_ZeroSseMaxReconnectAttempts_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseMaxReconnectAttempts = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Validate - DefaultInboxTtlSeconds Validation

    [Fact]
    public void Validate_TooSmallDefaultInboxTtlSeconds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = 59;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("DefaultInboxTtlSeconds must be at least 60 seconds");
    }

    [Fact]
    public void Validate_MinimumDefaultInboxTtlSeconds_ShouldNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = 60;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroDefaultInboxTtlSeconds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = 0;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("DefaultInboxTtlSeconds must be at least 60 seconds");
    }

    [Fact]
    public void Validate_NegativeDefaultInboxTtlSeconds_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = -1;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("DefaultInboxTtlSeconds must be at least 60 seconds");
    }

    #endregion

    #region Validate - DeliveryStrategy Validation

    [Theory]
    [InlineData(DeliveryStrategy.Sse)]
    [InlineData(DeliveryStrategy.Polling)]
    public void Validate_AllDeliveryStrategies_ShouldNotThrow(DeliveryStrategy strategy)
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultDeliveryStrategy = strategy;

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private static VaultSandboxClientOptions CreateValidOptions()
    {
        return new VaultSandboxClientOptions
        {
            BaseUrl = "https://smtp.vaultsandbox.com",
            ApiKey = "test-api-key-12345"
        };
    }

    #endregion
}
