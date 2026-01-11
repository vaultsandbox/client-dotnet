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
    public async Task ExportInbox_ShouldReturnValidExportData()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var export = await inbox.ExportAsync();

        // Assert
        export.Should().NotBeNull();
        export.Version.Should().Be(1);
        export.EmailAddress.Should().Be(inbox.EmailAddress);
        export.InboxHash.Should().Be(inbox.InboxHash);
        export.ExpiresAt.Should().Be(inbox.ExpiresAt);
        export.SecretKey.Should().NotBeNullOrEmpty();
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

    [SkippableFact]
    public async Task GetEmailsMetadataOnly_EmptyInbox_ShouldReturnEmptyList()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var metadata = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadata.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task GetEmailsMetadataOnly_WithEmails_ShouldReturnMetadataWithoutBody()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Metadata Test {Guid.NewGuid():N}";
        var body = "This body should not be in metadata response";
        var fromAddress = "metadata-sender@test.example.com";

        await SendTestEmailAsync(inbox.EmailAddress, subject, body, from: fromAddress);

        // Wait for email to arrive
        await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var metadataList = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadataList.Should().HaveCount(1);
        var metadata = metadataList[0];
        metadata.Id.Should().NotBeNullOrEmpty();
        metadata.Subject.Should().Be(subject);
        metadata.From.Should().Contain(fromAddress);
        metadata.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        metadata.IsRead.Should().BeFalse();
    }

    [SkippableFact]
    public async Task GetEmailsMetadataOnly_WithMultipleEmails_ShouldReturnAllMetadata()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subjects = new[]
        {
            $"Metadata 1 - {Guid.NewGuid():N}",
            $"Metadata 2 - {Guid.NewGuid():N}",
            $"Metadata 3 - {Guid.NewGuid():N}"
        };

        // Send multiple emails
        foreach (var subject in subjects)
        {
            await SendTestEmailAsync(inbox.EmailAddress, subject, $"Body for {subject}");
        }

        // Wait for all emails to arrive
        await Task.Delay(2000);
        await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subjects[^1],
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var metadataList = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert
        metadataList.Should().HaveCountGreaterThanOrEqualTo(3);
        foreach (var subject in subjects)
        {
            metadataList.Should().Contain(m => m.Subject == subject);
        }
    }

    [SkippableFact]
    public async Task GetEmailsMetadataOnly_ShouldMatchFullEmailData()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Metadata Comparison {Guid.NewGuid():N}";
        var fromAddress = "comparison-test@example.com";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body", from: fromAddress);

        var fullEmail = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var metadataList = await inbox.GetEmailsMetadataOnlyAsync();

        // Assert - Metadata should match the full email properties
        var metadata = metadataList.Single(m => m.Id == fullEmail.Id);
        metadata.Subject.Should().Be(fullEmail.Subject);
        metadata.From.Should().Be(fullEmail.From);
        metadata.ReceivedAt.Should().Be(fullEmail.ReceivedAt);
        metadata.IsRead.Should().Be(fullEmail.IsRead);
    }

    [SkippableFact]
    public async Task GetEmailsMetadataOnly_AfterMarkAsRead_ShouldReflectReadStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Read Status Metadata {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Verify initially unread
        var initialMetadata = await inbox.GetEmailsMetadataOnlyAsync();
        initialMetadata.Single(m => m.Id == email.Id).IsRead.Should().BeFalse();

        // Act - Mark as read
        await inbox.MarkAsReadAsync(email.Id);

        // Assert - Metadata should reflect read status
        var updatedMetadata = await inbox.GetEmailsMetadataOnlyAsync();
        updatedMetadata.Single(m => m.Id == email.Id).IsRead.Should().BeTrue();
    }
}
