using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit;

public class VaultSandboxClientOptionsValidatorTests
{
    private readonly VaultSandboxClientOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ShouldReturnSuccess()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EmptyBaseUrl_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseUrl is required");
    }

    [Fact]
    public void Validate_WhitespaceBaseUrl_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "   ";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseUrl is required");
    }

    [Fact]
    public void Validate_InvalidBaseUrl_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "not-a-valid-url";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseUrl must be a valid HTTP(S) URL");
    }

    [Fact]
    public void Validate_NonHttpBaseUrl_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "ftp://example.com";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseUrl must be a valid HTTP(S) URL");
    }

    [Fact]
    public void Validate_HttpBaseUrl_ShouldSucceed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "http://localhost:3000";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_HttpsBaseUrl_ShouldSucceed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "https://smtp.vaultsandbox.com";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EmptyApiKey_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "";

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ApiKey is required");
    }

    [Fact]
    public void Validate_ZeroHttpTimeout_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HttpTimeoutMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HttpTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_NegativeHttpTimeout_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HttpTimeoutMs = -1;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HttpTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_ZeroWaitTimeout_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.WaitTimeoutMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("WaitTimeoutMs must be positive");
    }

    [Fact]
    public void Validate_ZeroPollInterval_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.PollIntervalMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("PollIntervalMs must be positive");
    }

    [Fact]
    public void Validate_NegativeMaxRetries_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRetries = -1;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxRetries cannot be negative");
    }

    [Fact]
    public void Validate_ZeroMaxRetries_ShouldSucceed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRetries = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_ZeroRetryDelay_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.RetryDelayMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("RetryDelayMs must be positive");
    }

    [Fact]
    public void Validate_ZeroSseReconnectInterval_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseReconnectIntervalMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SseReconnectIntervalMs must be positive");
    }

    [Fact]
    public void Validate_NegativeSseMaxReconnectAttempts_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseMaxReconnectAttempts = -1;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SseMaxReconnectAttempts cannot be negative");
    }

    [Fact]
    public void Validate_ZeroSseMaxReconnectAttempts_ShouldSucceed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.SseMaxReconnectAttempts = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_TooSmallInboxTtl_ShouldFail()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = 59;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultInboxTtlSeconds must be at least 60 seconds");
    }

    [Fact]
    public void Validate_MinimumInboxTtl_ShouldSucceed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.DefaultInboxTtlSeconds = 60;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_MultipleErrors_ShouldReportAll()
    {
        // Arrange
        var options = CreateValidOptions();
        options.BaseUrl = "";
        options.ApiKey = "";
        options.HttpTimeoutMs = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("BaseUrl is required");
        result.FailureMessage.Should().Contain("ApiKey is required");
        result.FailureMessage.Should().Contain("HttpTimeoutMs must be positive");
    }

    private static VaultSandboxClientOptions CreateValidOptions()
    {
        return new VaultSandboxClientOptions
        {
            BaseUrl = "https://smtp.vaultsandbox.com",
            ApiKey = "test-api-key-12345"
        };
    }
}
