using FluentAssertions;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for VaultSandboxApiClient against a real server.
/// Tests require a configured .env file with valid credentials.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ApiClientIntegrationTests : IAsyncLifetime
{
    private readonly TestSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly VaultSandboxApiClient _apiClient;
    private readonly CryptoProvider _cryptoProvider;
    private readonly List<string> _createdInboxes = [];

    public ApiClientIntegrationTests()
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
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up any created inboxes
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
    public async Task CheckKey_ValidApiKey_ShouldReturnOk()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Act
        var response = await _apiClient.CheckKeyAsync();

        // Assert
        response.Ok.Should().BeTrue();
    }

    [SkippableFact]
    public async Task CheckKey_InvalidApiKey_ShouldThrowUnauthorized()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var badHandler = new ApiKeyAuthenticationHandler("invalid-key")
        {
            InnerHandler = new HttpClientHandler()
        };
        using var badClient = new HttpClient(badHandler) { BaseAddress = new Uri(_settings.BaseUrl) };
        using var badApiClient = new VaultSandboxApiClient(badClient);

        // Act
        Func<Task> act = () => badApiClient.CheckKeyAsync();

        // Assert
        await act.Should().ThrowAsync<ApiException>()
            .Where(e => e.StatusCode == 401);
    }

    [SkippableFact]
    public async Task GetServerInfo_ShouldReturnValidConfiguration()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Act
        var response = await _apiClient.GetServerInfoAsync();

        // Assert
        response.Should().NotBeNull();
        response.ServerSigPk.Should().NotBeNullOrEmpty();
        response.Context.Should().Be("vaultsandbox:email:v1");
        response.Algorithms.Should().NotBeNull();
        response.Algorithms.Kem.Should().Be("ML-KEM-768");
        response.Algorithms.Sig.Should().Be("ML-DSA-65");
        response.Algorithms.Aead.Should().Be("AES-256-GCM");
        response.Algorithms.Kdf.Should().Be("HKDF-SHA-512");
        response.MaxTtl.Should().BeGreaterThan(0);
        response.DefaultTtl.Should().BeGreaterThan(0);
        response.AllowedDomains.Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task CreateInbox_ShouldReturnNewInbox()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var request = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300 // 5 minutes
        };

        // Act
        var response = await _apiClient.CreateInboxAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.EmailAddress.Should().NotBeNullOrEmpty();
        response.EmailAddress.Should().Contain("@");
        response.InboxHash.Should().NotBeNullOrEmpty();
        response.ServerSigPk.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Track for cleanup
        _createdInboxes.Add(response.EmailAddress);
    }

    [SkippableFact]
    public async Task CreateInbox_WithCustomEmail_ShouldReturnSpecifiedEmail()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var serverInfo = await _apiClient.GetServerInfoAsync();
        var domain = serverInfo.AllowedDomains.First();
        var customEmail = $"test-{Guid.NewGuid():N}@{domain}";

        var request = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300,
            EmailAddress = customEmail
        };

        // Act
        var response = await _apiClient.CreateInboxAsync(request);

        // Assert
        response.EmailAddress.Should().Be(customEmail);

        // Track for cleanup
        _createdInboxes.Add(response.EmailAddress);
    }

    [SkippableFact]
    public async Task GetInboxSync_ExistingInbox_ShouldReturnSyncStatus()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var createRequest = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };
        var inbox = await _apiClient.CreateInboxAsync(createRequest);
        _createdInboxes.Add(inbox.EmailAddress);

        // Act
        var sync = await _apiClient.GetInboxSyncAsync(inbox.EmailAddress);

        // Assert
        sync.Should().NotBeNull();
        sync.EmailCount.Should().Be(0); // New inbox should have no emails
        sync.EmailsHash.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task GetInboxSync_NonExistentInbox_ShouldThrowInboxNotFoundException()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var nonExistentEmail = $"nonexistent-{Guid.NewGuid():N}@example.com";

        // Act
        Func<Task> act = () => _apiClient.GetInboxSyncAsync(nonExistentEmail);

        // Assert
        await act.Should().ThrowAsync<InboxNotFoundException>();
    }

    [SkippableFact]
    public async Task GetEmails_EmptyInbox_ShouldReturnEmptyArray()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var createRequest = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };
        var inbox = await _apiClient.CreateInboxAsync(createRequest);
        _createdInboxes.Add(inbox.EmailAddress);

        // Act
        var emails = await _apiClient.GetEmailsAsync(inbox.EmailAddress);

        // Assert
        emails.Should().NotBeNull();
        emails.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task DeleteInbox_ExistingInbox_ShouldSucceed()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var createRequest = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };
        var inbox = await _apiClient.CreateInboxAsync(createRequest);

        // Act
        Func<Task> act = () => _apiClient.DeleteInboxAsync(inbox.EmailAddress);

        // Assert
        await act.Should().NotThrowAsync();

        // Verify inbox no longer exists
        Func<Task> getAct = () => _apiClient.GetInboxSyncAsync(inbox.EmailAddress);
        await getAct.Should().ThrowAsync<InboxNotFoundException>();
    }

    [SkippableFact]
    public async Task DeleteAllInboxes_ShouldReturnDeletedCount()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange - Create a couple of inboxes
        var keyPair1 = _cryptoProvider.GenerateKeyPair();
        var keyPair2 = _cryptoProvider.GenerateKeyPair();

        await _apiClient.CreateInboxAsync(new CreateInboxRequest
        {
            ClientKemPk = keyPair1.PublicKeyB64,
            Ttl = 300
        });

        await _apiClient.CreateInboxAsync(new CreateInboxRequest
        {
            ClientKemPk = keyPair2.PublicKeyB64,
            Ttl = 300
        });

        // Act
        var response = await _apiClient.DeleteAllInboxesAsync();

        // Assert
        response.Should().NotBeNull();
        response.Deleted.Should().BeGreaterThanOrEqualTo(2);

        // Clear cleanup list since we just deleted everything
        _createdInboxes.Clear();
    }

    [SkippableFact]
    public async Task GetEmail_NonExistent_ShouldThrowEmailNotFoundException()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var createRequest = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };
        var inbox = await _apiClient.CreateInboxAsync(createRequest);
        _createdInboxes.Add(inbox.EmailAddress);

        // Act
        Func<Task> act = () => _apiClient.GetEmailAsync(inbox.EmailAddress, "nonexistent-id");

        // Assert
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }

    [SkippableFact]
    public async Task GetRawEmail_NonExistent_ShouldThrowEmailNotFoundException()
    {
        Skip.IfNot(_settings.IsConfigured, "Integration tests require .env configuration");

        // Arrange
        var keyPair = _cryptoProvider.GenerateKeyPair();
        var createRequest = new CreateInboxRequest
        {
            ClientKemPk = keyPair.PublicKeyB64,
            Ttl = 300
        };
        var inbox = await _apiClient.CreateInboxAsync(createRequest);
        _createdInboxes.Add(inbox.EmailAddress);

        // Act
        Func<Task> act = () => _apiClient.GetRawEmailAsync(inbox.EmailAddress, "nonexistent-id");

        // Assert
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }
}
