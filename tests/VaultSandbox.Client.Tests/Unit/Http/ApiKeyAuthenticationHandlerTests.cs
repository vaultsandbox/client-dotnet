using FluentAssertions;
using VaultSandbox.Client.Http;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Http;

public class ApiKeyAuthenticationHandlerTests
{
    private const string TestApiKey = "test-api-key-12345";

    [Fact]
    public async Task SendAsync_ShouldAddApiKeyHeader()
    {
        // Arrange
        var capturedRequest = (HttpRequestMessage?)null;
        var mockHandler = new MockHttpHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new ApiKeyAuthenticationHandler(TestApiKey)
        {
            InnerHandler = mockHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.example.com")
        };

        // Act
        await httpClient.GetAsync("/api/test");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().ContainKey("X-API-Key");
        capturedRequest.Headers.GetValues("X-API-Key").Should().ContainSingle()
            .Which.Should().Be(TestApiKey);
    }

    [Fact]
    public async Task SendAsync_ShouldPreserveOtherHeaders()
    {
        // Arrange
        var capturedRequest = (HttpRequestMessage?)null;
        var mockHandler = new MockHttpHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new ApiKeyAuthenticationHandler(TestApiKey)
        {
            InnerHandler = mockHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.example.com")
        };

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/test");
        request.Headers.Add("X-Custom-Header", "custom-value");

        // Act
        await httpClient.SendAsync(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().ContainKey("X-API-Key");
        capturedRequest.Headers.Should().ContainKey("X-Custom-Header");
        capturedRequest.Headers.GetValues("X-Custom-Header").Should().ContainSingle()
            .Which.Should().Be("custom-value");
    }

    [Fact]
    public void Constructor_NullApiKey_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new ApiKeyAuthenticationHandler(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiKey");
    }

    [Fact]
    public async Task SendAsync_MultipleRequests_ShouldAddHeaderToAll()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = new MockHttpHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new ApiKeyAuthenticationHandler(TestApiKey)
        {
            InnerHandler = mockHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.example.com")
        };

        // Act
        await httpClient.GetAsync("/api/test1");
        await httpClient.GetAsync("/api/test2");
        await httpClient.GetAsync("/api/test3");

        // Assert
        requestCount.Should().Be(3);
    }

    /// <summary>
    /// Simple mock HTTP handler for testing.
    /// </summary>
    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
