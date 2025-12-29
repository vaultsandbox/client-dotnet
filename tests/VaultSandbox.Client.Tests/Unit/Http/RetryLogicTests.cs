using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Extensions;
using VaultSandbox.Client.Http;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Http;

/// <summary>
/// Tests for HTTP retry logic behavior.
/// Verifies that the client retries on transient failures (5xx) and does not retry on client errors (4xx).
/// </summary>
public class RetryLogicTests
{
    [Fact]
    public async Task Request_With5xxError_ShouldRetryAndEventuallySucceed()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(request =>
        {
            callCount++;
            if (callCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("Service unavailable")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok": true}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10; // Short delay for tests
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        var result = await apiClient.CheckKeyAsync();

        // Assert
        result.Ok.Should().BeTrue();
        callCount.Should().Be(3, "should have retried twice before succeeding");
    }

    [Fact]
    public async Task Request_WithMaxRetriesExceeded_ShouldThrowAfterAllAttempts()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal Server Error")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 2;
            options.RetryDelayMs = 10; // Short delay for tests
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        Func<Task> act = () => apiClient.CheckKeyAsync();

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(ex => ex.StatusCode == 500);

        // Initial attempt + 2 retries = 3 total calls
        callCount.Should().Be(3, "should have made initial attempt plus 2 retries");
    }

    [Fact]
    public async Task Request_With4xxError_ShouldNotRetry()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10;
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        Func<Task> act = () => apiClient.CheckKeyAsync();

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(ex => ex.StatusCode == 400);

        // 4xx errors should not be retried
        callCount.Should().Be(1, "4xx errors should not trigger retries");
    }

    [Fact]
    public async Task Request_With401Unauthorized_ShouldNotRetry()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("Unauthorized")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "invalid-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10;
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        Func<Task> act = () => apiClient.CheckKeyAsync();

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(ex => ex.StatusCode == 401);

        callCount.Should().Be(1, "401 errors should not trigger retries");
    }

    [Fact]
    public async Task Request_With404NotFound_ShouldNotRetry()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not Found")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10;
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        Func<Task> act = () => apiClient.GetInboxSyncAsync("nonexistent@example.com");

        // Assert
        await act.Should().ThrowAsync<InboxNotFoundException>();

        callCount.Should().Be(1, "404 errors should not trigger retries");
    }

    [Fact]
    public async Task Request_With429TooManyRequests_ShouldRetry()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            if (callCount < 2)
            {
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent("Too Many Requests")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok": true}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10;
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        var result = await apiClient.CheckKeyAsync();

        // Assert
        result.Ok.Should().BeTrue();
        callCount.Should().Be(2, "should have retried once after 429");
    }

    [Fact]
    public async Task Request_With502BadGateway_ShouldRetry()
    {
        // Arrange
        var callCount = 0;
        var handler = new MockHttpHandler(_ =>
        {
            callCount++;
            if (callCount < 2)
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("Bad Gateway")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok": true}""", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var services = new ServiceCollection();
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
            options.MaxRetries = 3;
            options.RetryDelayMs = 10;
        });

        // Replace the HTTP handler with our mock
        services.AddHttpClient("VaultSandboxApiClient")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IVaultSandboxApiClient>();

        // Act
        var result = await apiClient.CheckKeyAsync();

        // Assert
        result.Ok.Should().BeTrue();
        callCount.Should().Be(2, "should have retried once after 502");
    }

    /// <summary>
    /// Mock HTTP handler that allows custom response generation.
    /// </summary>
    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
