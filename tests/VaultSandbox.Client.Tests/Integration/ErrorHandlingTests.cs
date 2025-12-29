using System.Net.Http;
using FluentAssertions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Tests for error handling scenarios including network errors.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ErrorHandlingTests
{
    [Fact]
    public async Task ConnectToNonExistentServer_ShouldThrowException()
    {
        // Arrange - Use a non-routable IP address to simulate network failure
        var nonExistentUrl = "http://192.0.2.1:9999"; // TEST-NET-1, guaranteed not to route

        // Act
        Func<Task> act = async () =>
        {
            await VaultSandboxClientBuilder.Create()
                .WithBaseUrl(nonExistentUrl)
                .WithApiKey("test-api-key")
                .WithHttpTimeout(TimeSpan.FromSeconds(5))
                .BuildAndValidateAsync();
        };

        // Assert - Connection to unreachable host throws TaskCanceledException (timeout)
        // or HttpRequestException (connection refused) depending on network config
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Should().Match<Exception>(e =>
            e is HttpRequestException || e is TaskCanceledException);
    }

    [Fact]
    public async Task ConnectToInvalidHost_ShouldThrowHttpRequestException()
    {
        // Arrange - Use an invalid hostname
        var invalidUrl = "http://this-host-definitely-does-not-exist.invalid:3000";

        // Act
        Func<Task> act = async () =>
        {
            await VaultSandboxClientBuilder.Create()
                .WithBaseUrl(invalidUrl)
                .WithApiKey("test-api-key")
                .WithHttpTimeout(TimeSpan.FromSeconds(5))
                .BuildAndValidateAsync();
        };

        // Assert - DNS resolution failures manifest as HttpRequestException
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
