using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for import/export file operations.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ImportExportFileTests : IntegrationTestBase, IDisposable
{
    private readonly List<string> _tempFiles = [];

    private string CreateTempFile(string? content = null)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"inbox-export-{Guid.NewGuid()}.json");
        _tempFiles.Add(filePath);

        if (content != null)
        {
            File.WriteAllText(filePath, content);
        }

        return filePath;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [SkippableFact]
    public async Task ExportInboxToFile_ShouldCreateValidJsonFile()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var filePath = CreateTempFile();

        // Act
        await Client.ExportInboxToFileAsync(inbox, filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue("export file should be created");

        var content = await File.ReadAllTextAsync(filePath);
        content.Should().NotBeNullOrEmpty();

        // Verify it's valid JSON by parsing it
        var parsed = System.Text.Json.JsonDocument.Parse(content);
        parsed.RootElement.TryGetProperty("emailAddress", out var emailProp).Should().BeTrue();
        emailProp.GetString().Should().Be(inbox.EmailAddress);
    }

    [SkippableFact]
    public async Task ImportInboxFromFile_ShouldReturnFunctionalInbox()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var original = await Client.CreateInboxAsync();
        var filePath = CreateTempFile();

        // Export the inbox to file
        await Client.ExportInboxToFileAsync(original, filePath);

        // Act
        await using var restored = await Client.ImportInboxFromFileAsync(filePath);

        // Assert
        restored.Should().NotBeNull();
        restored.EmailAddress.Should().Be(original.EmailAddress);
        restored.InboxHash.Should().Be(original.InboxHash);
        restored.ExpiresAt.Should().Be(original.ExpiresAt);

        // Verify the restored inbox is functional by getting email count
        var count = await restored.GetEmailCountAsync();
        count.Should().Be(0);
    }

    [SkippableFact]
    public async Task ImportInboxFromFile_InvalidJson_ShouldThrowInvalidImportDataException()
    {
        SkipIfNotConfigured();

        // Arrange
        var filePath = CreateTempFile("{ this is not valid json }");

        // Act
        Func<Task> act = () => Client.ImportInboxFromFileAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<InvalidImportDataException>();
    }

    [SkippableFact]
    public async Task ImportInboxFromFile_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        SkipIfNotConfigured();

        // Arrange
        var filePath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}.json");

        // Act
        Func<Task> act = () => Client.ImportInboxFromFileAsync(filePath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [SkippableFact]
    public async Task ExportInboxToFile_ShouldWriteFormattedJson()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var filePath = CreateTempFile();

        // Act
        await Client.ExportInboxToFileAsync(inbox, filePath);

        // Assert
        var content = await File.ReadAllTextAsync(filePath);

        // Formatted JSON should contain newlines and indentation
        content.Should().Contain("\n", "formatted JSON should contain newlines");
        content.Should().Contain("  ", "formatted JSON should contain indentation");

        // Also verify structure by checking for properly formatted properties
        var lines = content.Split('\n');
        lines.Length.Should().BeGreaterThan(1, "formatted JSON should span multiple lines");
    }
}
