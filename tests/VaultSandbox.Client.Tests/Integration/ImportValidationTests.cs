using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for import validation.
/// Tests various invalid import data scenarios.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ImportValidationTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task ImportInbox_MissingEmailAddress_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty email address
        var invalidExport = export with { EmailAddress = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*EmailAddress*");
    }

    [SkippableFact]
    public async Task ImportInbox_EmptyEmailAddress_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with empty email address
        var invalidExport = export with { EmailAddress = "" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*EmailAddress*");
    }

    [SkippableFact]
    public async Task ImportInbox_MissingPublicKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty public key
        var invalidExport = export with { PublicKeyB64 = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*PublicKeyB64*");
    }

    [SkippableFact]
    public async Task ImportInbox_EmptyPublicKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with empty public key
        var invalidExport = export with { PublicKeyB64 = "" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*PublicKeyB64*");
    }

    [SkippableFact]
    public async Task ImportInbox_MissingSecretKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty secret key
        var invalidExport = export with { SecretKeyB64 = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*SecretKeyB64*");
    }

    [SkippableFact]
    public async Task ImportInbox_EmptySecretKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with empty secret key
        var invalidExport = export with { SecretKeyB64 = "" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*SecretKeyB64*");
    }

    [SkippableFact]
    public async Task ImportInbox_InvalidBase64PublicKey_ShouldThrowException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with invalid base64 in public key
        var invalidExport = export with { PublicKeyB64 = "!!!invalid-base64!!!" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert - Should throw FormatException from Base64 decode
        await act.Should().ThrowAsync<FormatException>();
    }

    [SkippableFact]
    public async Task ImportInbox_InvalidBase64SecretKey_ShouldThrowException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with invalid base64 in secret key
        var invalidExport = export with { SecretKeyB64 = "!!!invalid-base64!!!" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert - Should throw FormatException from Base64 decode
        await act.Should().ThrowAsync<FormatException>();
    }

    [SkippableFact]
    public async Task ImportInbox_WrongPublicKeyLength_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with wrong-sized public key (valid base64 but wrong length)
        // ML-KEM-768 public key should be 1184 bytes, using shorter key
        var shortKey = Convert.ToBase64String(new byte[100]);
        var invalidExport = export with { PublicKeyB64 = shortKey };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*public key size*");
    }

    [SkippableFact]
    public async Task ImportInbox_WrongSecretKeyLength_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with wrong-sized secret key (valid base64 but wrong length)
        // ML-KEM-768 secret key should be 2400 bytes, using shorter key
        var shortKey = Convert.ToBase64String(new byte[100]);
        var invalidExport = export with { SecretKeyB64 = shortKey };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*secret key size*");
    }

    [SkippableFact]
    public async Task ImportInbox_ExpiredTimestamp_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with expired timestamp
        var expiredExport = export with { ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(expiredExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*expired*");
    }
}
