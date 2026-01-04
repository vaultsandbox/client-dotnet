using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for import validation.
/// Tests various invalid import data scenarios per VaultSandbox spec Section 10.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ImportValidationTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task ImportInbox_UnsupportedVersion_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with unsupported version (per spec, must be 1)
        var invalidExport = export with { Version = 2 };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*Unsupported export version*");
    }

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
    public async Task ImportInbox_EmailWithoutAtSymbol_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Per spec Section 10.1: email must contain exactly one @ character
        var invalidExport = export with { EmailAddress = "invalid-no-at-symbol" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*exactly one '@'*");
    }

    [SkippableFact]
    public async Task ImportInbox_EmailWithMultipleAtSymbols_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Per spec Section 10.1: email must contain exactly one @ character
        var invalidExport = export with { EmailAddress = "invalid@@multiple.at" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*exactly one '@'*");
    }

    [SkippableFact]
    public async Task ImportInbox_MissingSecretKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty secret key
        var invalidExport = export with { SecretKey = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*SecretKey*");
    }

    [SkippableFact]
    public async Task ImportInbox_EmptySecretKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with empty secret key
        var invalidExport = export with { SecretKey = "" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*SecretKey*");
    }

    [SkippableFact]
    public async Task ImportInbox_InvalidBase64SecretKey_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Per spec Section 2.2: Base64URL MUST reject +, /, or =
        var invalidExport = export with { SecretKey = "invalid+base64/with=padding" };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert - Should throw InvalidImportDataException wrapping the FormatException
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*Invalid SecretKey encoding*");
    }

    [SkippableFact]
    public async Task ImportInbox_WrongSecretKeyLength_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with wrong-sized secret key (valid base64url but wrong length)
        // ML-KEM-768 secret key should be 2400 bytes, using shorter key
        var shortKey = Base64Url.Encode(new byte[100]);
        var invalidExport = export with { SecretKey = shortKey };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*secret key size*");
    }

    [SkippableFact]
    public async Task ImportInbox_MissingServerSigPk_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty server sig pk
        var invalidExport = export with { ServerSigPk = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*ServerSigPk*");
    }

    [SkippableFact]
    public async Task ImportInbox_WrongServerSigPkLength_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with wrong-sized server sig pk (valid base64url but wrong length)
        // ML-DSA-65 public key should be 1952 bytes, using shorter key
        var shortKey = Base64Url.Encode(new byte[100]);
        var invalidExport = export with { ServerSigPk = shortKey };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*server signature public key size*");
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

    [SkippableFact]
    public async Task ImportInbox_MissingInboxHash_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Create export with null/empty inbox hash
        var invalidExport = export with { InboxHash = null! };

        // Act
        Func<Task> act = () => Client.ImportInboxAsync(invalidExport);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>()
            .WithMessage("*InboxHash*");
    }

    [SkippableFact]
    public async Task ImportInbox_ValidExport_ShouldSucceed()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var export = await original.ExportAsync();

        // Act - import the valid export
        await using var imported = await Client.ImportInboxAsync(export);

        // Assert
        imported.EmailAddress.Should().Be(original.EmailAddress);
        imported.InboxHash.Should().Be(original.InboxHash);
        imported.ExpiresAt.Should().Be(original.ExpiresAt);
    }
}
