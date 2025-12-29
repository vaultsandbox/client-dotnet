using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for inbox operations using the full client.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class InboxIntegrationTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task CreateInbox_ShouldReturnValidInbox()
    {
        SkipIfNotConfigured();

        // Act
        await using var inbox = await Client.CreateInboxAsync();

        // Assert
        inbox.EmailAddress.Should().NotBeNullOrEmpty();
        inbox.EmailAddress.Should().Contain("@");
        inbox.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        inbox.InboxHash.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task CreateInbox_WithCustomTtl_ShouldSetExpiration()
    {
        SkipIfNotConfigured();

        // Arrange
        var ttl = TimeSpan.FromMinutes(5);

        // Act
        await using var inbox = await Client.CreateInboxAsync(new CreateInboxOptions { Ttl = ttl });

        // Assert
        var expectedExpiry = DateTimeOffset.UtcNow.Add(ttl);
        inbox.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(30));
    }

    [SkippableFact]
    public async Task CreateInbox_WithCustomEmailAddress_ShouldUseProvidedAddress()
    {
        SkipIfNotConfigured();

        // Arrange
        var serverInfo = await Client.GetServerInfoAsync();
        var domain = serverInfo.AllowedDomains.First();
        var customEmail = $"custom-{Guid.NewGuid():N}@{domain}";

        // Act
        await using var inbox = await Client.CreateInboxAsync(new CreateInboxOptions
        {
            EmailAddress = customEmail
        });

        // Assert
        inbox.EmailAddress.Should().Be(customEmail);
    }

    [SkippableFact]
    public async Task CreateMultipleInboxes_ShouldSucceed()
    {
        SkipIfNotConfigured();

        // Act
        await using var inbox1 = await Client.CreateInboxAsync();
        await using var inbox2 = await Client.CreateInboxAsync();
        await using var inbox3 = await Client.CreateInboxAsync();

        // Assert
        inbox1.EmailAddress.Should().NotBe(inbox2.EmailAddress);
        inbox2.EmailAddress.Should().NotBe(inbox3.EmailAddress);
        inbox1.InboxHash.Should().NotBe(inbox2.InboxHash);
    }

    [SkippableFact]
    public async Task DeleteInbox_ExistingInbox_ShouldRemoveInbox()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var emailAddress = inbox.EmailAddress;

        // Act
        await Client.DeleteInboxAsync(emailAddress);

        // Assert - Subsequent operations should fail
        Func<Task> act = () => inbox.GetEmailsAsync();
        await act.Should().ThrowAsync<InboxNotFoundException>();
    }

    [SkippableFact]
    public async Task DeleteAllInboxes_ShouldRemoveAllInboxes()
    {
        SkipIfNotConfigured();

        // Arrange
        var inbox1 = await Client.CreateInboxAsync();
        var inbox2 = await Client.CreateInboxAsync();

        // Act
        var deletedCount = await Client.DeleteAllInboxesAsync();

        // Assert
        deletedCount.Should().BeGreaterThanOrEqualTo(2);

        // Dispose without error since inboxes are already deleted
        await inbox1.DisposeAsync();
        await inbox2.DisposeAsync();
    }

    [SkippableFact]
    public async Task ExportInbox_ShouldReturnValidExportData()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var export = await inbox.ExportAsync();

        // Assert
        export.Should().NotBeNull();
        export.EmailAddress.Should().Be(inbox.EmailAddress);
        export.InboxHash.Should().Be(inbox.InboxHash);
        export.ExpiresAt.Should().Be(inbox.ExpiresAt);
        export.PublicKeyB64.Should().NotBeNullOrEmpty();
        export.SecretKeyB64.Should().NotBeNullOrEmpty();
        export.ServerSigPk.Should().NotBeNullOrEmpty();
        export.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [SkippableFact]
    public async Task ImportInbox_ShouldRestoreInbox()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Act
        await using var restored = await Client.ImportInboxAsync(export);

        // Assert
        restored.EmailAddress.Should().Be(original.EmailAddress);
        restored.InboxHash.Should().Be(original.InboxHash);
        restored.ExpiresAt.Should().Be(original.ExpiresAt);
    }

    [SkippableFact]
    public async Task ImportInbox_ExpiredExport_ShouldThrow()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create an expired export
        var expiredExport = export with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(expiredExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>();
    }

    [SkippableFact]
    public async Task ValidateApiKey_ValidKey_ShouldReturnTrue()
    {
        SkipIfNotConfigured();

        // Act
        var isValid = await Client.ValidateApiKeyAsync();

        // Assert
        isValid.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetServerInfo_ShouldReturnValidConfiguration()
    {
        SkipIfNotConfigured();

        // Act
        var serverInfo = await Client.GetServerInfoAsync();

        // Assert
        serverInfo.Should().NotBeNull();
        serverInfo.ServerSigPk.Should().NotBeNullOrEmpty();
        serverInfo.Context.Should().Be("vaultsandbox:email:v1");
        serverInfo.MaxTtl.Should().BeGreaterThan(0);
        serverInfo.DefaultTtl.Should().BeGreaterThan(0);
        serverInfo.AllowedDomains.Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task InboxIsDisposed_ShouldBeTracked()
    {
        SkipIfNotConfigured();

        // Arrange
        var inbox = await Client.CreateInboxAsync();

        // Assert - before dispose
        inbox.IsDisposed.Should().BeFalse();

        // Act
        await inbox.DisposeAsync();

        // Assert - after dispose
        inbox.IsDisposed.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetEmailCount_NewInbox_ShouldReturnZero()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var count = await inbox.GetEmailCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [SkippableFact]
    public async Task ValidateApiKey_InvalidKey_ShouldReturnFalseOrThrow()
    {
        SkipIfNotConfigured();

        // Arrange - Create client with invalid API key
        await using var invalidClient = VaultSandboxClientBuilder.Create()
            .WithBaseUrl(Settings.BaseUrl)
            .WithApiKey("invalid-api-key-that-does-not-exist")
            .WithWaitTimeout(TimeSpan.FromSeconds(10))
            .Build();

        // Act
        var isValid = await invalidClient.ValidateApiKeyAsync();

        // Assert
        isValid.Should().BeFalse();
    }

    [SkippableFact]
    public async Task GetSyncStatus_ShouldReturnConsistentHash()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act - Call GetSyncStatusAsync multiple times
        var status1 = await inbox.GetSyncStatusAsync();
        var status2 = await inbox.GetSyncStatusAsync();
        var status3 = await inbox.GetSyncStatusAsync();

        // Assert - Hash should be consistent for empty inbox
        status1.EmailsHash.Should().NotBeNullOrEmpty();
        status1.EmailsHash.Should().Be(status2.EmailsHash);
        status2.EmailsHash.Should().Be(status3.EmailsHash);
        status1.EmailCount.Should().Be(0);
    }
}
