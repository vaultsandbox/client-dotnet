using System.Net;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using VaultSandbox.Client.Http;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Http;

public class HttpClientConfigurationTests
{
    #region AddVaultSandboxResilience Extension Method

    [Fact]
    public void AddVaultSandboxResilience_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();

        // Act
        var act = () =>
        {
            services.AddHttpClient("TestClient")
                .AddVaultSandboxResilience(options);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_ShouldReturnHttpClientBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();

        // Act
        var result = services.AddHttpClient("TestClient")
            .AddVaultSandboxResilience(options);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IHttpClientBuilder>();
    }

    [Fact]
    public void AddVaultSandboxResilience_WithCustomRetries_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        options.MaxRetries = 5;

        // Act
        var act = () =>
        {
            services.AddHttpClient("TestClient")
                .AddVaultSandboxResilience(options);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_WithCustomTimeout_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        options.HttpTimeoutMs = 60_000;

        // Act
        var act = () =>
        {
            services.AddHttpClient("TestClient")
                .AddVaultSandboxResilience(options);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_WithCustomRetryDelay_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        options.RetryDelayMs = 2_000;

        // Act
        var act = () =>
        {
            services.AddHttpClient("TestClient")
                .AddVaultSandboxResilience(options);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_WithZeroRetries_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        options.MaxRetries = 0;

        // Act
        var act = () =>
        {
            services.AddHttpClient("TestClient")
                .AddVaultSandboxResilience(options);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_ShouldBuildServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        services.AddHttpClient("TestClient")
            .AddVaultSandboxResilience(options);

        // Act
        var act = () => services.BuildServiceProvider();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddVaultSandboxResilience_ShouldCreateHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = CreateValidOptions();
        services.AddHttpClient("TestClient")
            .AddVaultSandboxResilience(options);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act
        var client = factory.CreateClient("TestClient");

        // Assert
        client.Should().NotBeNull();
    }

    #endregion

    #region ShouldRetry - Network Exceptions

    [Fact]
    public void ShouldRetry_WithException_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var outcome = Outcome.FromException<HttpResponseMessage>(new HttpRequestException("Network error"));

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithTimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var outcome = Outcome.FromException<HttpResponseMessage>(new TaskCanceledException("Timeout"));

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithOperationCanceledException_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var outcome = Outcome.FromException<HttpResponseMessage>(new OperationCanceledException("Cancelled"));

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ShouldRetry - Retryable HTTP Status Codes

    [Fact]
    public void ShouldRetry_WithRequestTimeout408_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithTooManyRequests429_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithInternalServerError500_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithBadGateway502_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.BadGateway);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithServiceUnavailable503_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WithGatewayTimeout504_ShouldReturnTrue()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ShouldRetry - Non-Retryable HTTP Status Codes

    [Fact]
    public void ShouldRetry_WithOk200_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithCreated201_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.Created);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithNoContent204_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithBadRequest400_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithUnauthorized401_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithForbidden403_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithNotFound404_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithMethodNotAllowed405_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithConflict409_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.Conflict);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithUnprocessableEntity422_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_WithNotImplemented501_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var response = new HttpResponseMessage(HttpStatusCode.NotImplemented);
        var outcome = Outcome.FromResult(response);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ShouldRetry - Edge Cases

    [Fact]
    public void ShouldRetry_WithNullResponse_ShouldReturnFalse()
    {
        // Arrange
        var shouldRetry = GetShouldRetryMethod();
        var outcome = Outcome.FromResult<HttpResponseMessage>(null!);

        // Act
        var result = (bool)shouldRetry.Invoke(null, new object[] { outcome })!;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static MethodInfo GetShouldRetryMethod()
    {
        var type = typeof(HttpClientConfiguration);
        var method = type.GetMethod("ShouldRetry", BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
            throw new InvalidOperationException("Could not find ShouldRetry method");

        return method;
    }

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
