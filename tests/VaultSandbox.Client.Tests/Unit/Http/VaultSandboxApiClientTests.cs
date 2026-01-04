using System.Net;
using System.Text.Json;
using FluentAssertions;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Http;

public class VaultSandboxApiClientTests : IDisposable
{
    private readonly MockHttpHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly VaultSandboxApiClient _apiClient;

    public VaultSandboxApiClientTests()
    {
        _mockHandler = new MockHttpHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://test.example.com")
        };
        _apiClient = new VaultSandboxApiClient(_httpClient);
    }

    public void Dispose()
    {
        _apiClient.Dispose();
        _httpClient.Dispose();
        _mockHandler.Dispose();
    }

    #region CheckKey Tests

    [Fact]
    public async Task CheckKeyAsync_Success_ShouldReturnResponse()
    {
        // Arrange
        _mockHandler.SetResponse("""{"ok": true}""", HttpStatusCode.OK);

        // Act
        var result = await _apiClient.CheckKeyAsync();

        // Assert
        result.Ok.Should().BeTrue();
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        _mockHandler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/api/check-key");
    }

    [Fact]
    public async Task CheckKeyAsync_Unauthorized_ShouldThrowApiException()
    {
        // Arrange
        _mockHandler.SetResponse("Unauthorized", HttpStatusCode.Unauthorized);

        // Act
        Func<Task> act = () => _apiClient.CheckKeyAsync();

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetServerInfo Tests

    [Fact]
    public async Task GetServerInfoAsync_Success_ShouldReturnResponse()
    {
        // Arrange
        var json = """
        {
            "serverSigPk": "base64encodedkey",
            "algs": {
                "kem": "ML-KEM-768",
                "sig": "ML-DSA-65",
                "aead": "AES-256-GCM",
                "kdf": "HKDF-SHA-512"
            },
            "context": "vaultsandbox:email:v1",
            "maxTtl": 86400,
            "defaultTtl": 3600,
            "sseConsole": true,
            "allowedDomains": ["example.com", "test.com"]
        }
        """;
        _mockHandler.SetResponse(json, HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetServerInfoAsync();

        // Assert
        result.ServerSigPk.Should().Be("base64encodedkey");
        result.Context.Should().Be("vaultsandbox:email:v1");
        result.Algorithms.Kem.Should().Be("ML-KEM-768");
        result.Algorithms.Sig.Should().Be("ML-DSA-65");
        result.MaxTtl.Should().Be(86400);
        result.DefaultTtl.Should().Be(3600);
        result.SseConsole.Should().BeTrue();
        result.AllowedDomains.Should().HaveCount(2);
    }

    #endregion

    #region CreateInbox Tests

    [Fact]
    public async Task CreateInboxAsync_Success_ShouldReturnResponse()
    {
        // Arrange
        var json = """
        {
            "emailAddress": "test@example.com",
            "expiresAt": "2024-12-31T23:59:59Z",
            "inboxHash": "hashvalue",
            "serverSigPk": "serverkey"
        }
        """;
        _mockHandler.SetResponse(json, HttpStatusCode.OK);

        var request = new CreateInboxRequest
        {
            ClientKemPk = "clientkey",
            Ttl = 3600
        };

        // Act
        var result = await _apiClient.CreateInboxAsync(request);

        // Assert
        result.EmailAddress.Should().Be("test@example.com");
        result.InboxHash.Should().Be("hashvalue");
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        _mockHandler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/api/inboxes");
    }

    #endregion

    #region DeleteInbox Tests

    [Fact]
    public async Task DeleteInboxAsync_Success_ShouldNotThrow()
    {
        // Arrange
        _mockHandler.SetResponse("", HttpStatusCode.NoContent);

        // Act
        Func<Task> act = () => _apiClient.DeleteInboxAsync("test@example.com");

        // Assert
        await act.Should().NotThrowAsync();
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task DeleteInboxAsync_ShouldUrlEncodeEmailAddress()
    {
        // Arrange
        _mockHandler.SetResponse("", HttpStatusCode.NoContent);

        // Act
        await _apiClient.DeleteInboxAsync("test+special@example.com");

        // Assert
        _mockHandler.LastRequest!.RequestUri!.PathAndQuery.Should().Contain("test%2Bspecial%40example.com");
    }

    #endregion

    #region GetInboxSync Tests

    [Fact]
    public async Task GetInboxSyncAsync_Success_ShouldReturnResponse()
    {
        // Arrange
        var json = """{"emailCount": 5, "emailsHash": "abc123"}""";
        _mockHandler.SetResponse(json, HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetInboxSyncAsync("test@example.com");

        // Assert
        result.EmailCount.Should().Be(5);
        result.EmailsHash.Should().Be("abc123");
    }

    [Fact]
    public async Task GetInboxSyncAsync_NotFound_ShouldThrowInboxNotFoundException()
    {
        // Arrange
        _mockHandler.SetResponse("Not found", HttpStatusCode.NotFound);

        // Act
        Func<Task> act = () => _apiClient.GetInboxSyncAsync("nonexistent@example.com");

        // Assert
        await act.Should().ThrowAsync<InboxNotFoundException>();
    }

    #endregion

    #region GetEmails Tests

    [Fact]
    public async Task GetEmailsAsync_Success_ShouldReturnArray()
    {
        // Arrange
        var json = """
        [
            {
                "id": "email1",
                "inboxId": "inbox1",
                "receivedAt": "2024-01-01T12:00:00Z",
                "isRead": false,
                "encryptedMetadata": {
                    "v": 1,
                    "algs": {"kem": "ML-KEM-768", "sig": "ML-DSA-65", "aead": "AES-256-GCM", "kdf": "HKDF-SHA-512"},
                    "ct_kem": "a",
                    "nonce": "b",
                    "aad": "c",
                    "ciphertext": "d",
                    "sig": "e",
                    "server_sig_pk": "f"
                },
                "encryptedParsed": {
                    "v": 1,
                    "algs": {"kem": "ML-KEM-768", "sig": "ML-DSA-65", "aead": "AES-256-GCM", "kdf": "HKDF-SHA-512"},
                    "ct_kem": "g",
                    "nonce": "h",
                    "aad": "i",
                    "ciphertext": "j",
                    "sig": "k",
                    "server_sig_pk": "l"
                }
            }
        ]
        """;
        _mockHandler.SetResponse(json, HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetEmailsAsync("test@example.com", includeContent: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("email1");
        result[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetEmailsAsync_EmptyInbox_ShouldReturnEmptyArray()
    {
        // Arrange
        _mockHandler.SetResponse("[]", HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetEmailsAsync("test@example.com", includeContent: false);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEmail Tests

    [Fact]
    public async Task GetEmailAsync_NotFound_ShouldThrowEmailNotFoundException()
    {
        // Arrange
        _mockHandler.SetResponse("Not found", HttpStatusCode.NotFound);

        // Act
        Func<Task> act = () => _apiClient.GetEmailAsync("test@example.com", "nonexistent");

        // Assert
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }

    #endregion

    #region DeleteAllInboxes Tests

    [Fact]
    public async Task DeleteAllInboxesAsync_Success_ShouldReturnDeletedCount()
    {
        // Arrange
        _mockHandler.SetResponse("""{"deleted": 3}""", HttpStatusCode.OK);

        // Act
        var result = await _apiClient.DeleteAllInboxesAsync();

        // Assert
        result.Deleted.Should().Be(3);
        _mockHandler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        _mockHandler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/api/inboxes");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendAsync_Forbidden_ShouldThrowApiException()
    {
        // Arrange
        _mockHandler.SetResponse("Forbidden", HttpStatusCode.Forbidden);

        // Act
        Func<Task> act = () => _apiClient.CheckKeyAsync();

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task SendAsync_ServerError_ShouldThrowApiException()
    {
        // Arrange
        _mockHandler.SetResponse("Internal Server Error", HttpStatusCode.InternalServerError);

        // Act
        Func<Task> act = () => _apiClient.CheckKeyAsync();

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(500);
    }

    #endregion

    /// <summary>
    /// Simple mock HTTP handler for testing.
    /// </summary>
    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _responseContent = "";

        public HttpRequestMessage? LastRequest { get; private set; }

        public void SetResponse(string content, HttpStatusCode statusCode)
        {
            _responseContent = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
