using System.Text;
using System.Text.Json;
using FluentAssertions;
using Moq;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Extensions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Extensions;

public class InboxExportExtensionsTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static InboxExport CreateTestExport() => new()
    {
        EmailAddress = "test@example.com",
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        InboxHash = "hash123abc",
        ServerSigPk = "server-sig-pk-base64",
        PublicKeyB64 = "public-key-base64",
        SecretKeyB64 = "secret-key-base64",
        ExportedAt = DateTimeOffset.UtcNow
    };

    #region ExportToStreamAsync Tests

    [Fact]
    public async Task ExportToStreamAsync_ShouldWriteJsonToStream()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        using var stream = new MemoryStream();

        // Act
        await mockInbox.Object.ExportToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        json.Should().Contain("test@example.com");
        json.Should().Contain("hash123abc");
    }

    [Fact]
    public async Task ExportToStreamAsync_ShouldProduceValidJson()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        using var stream = new MemoryStream();

        // Act
        await mockInbox.Object.ExportToStreamAsync(stream);

        // Assert
        stream.Position = 0;
        var deserialized = await JsonSerializer.DeserializeAsync<InboxExport>(stream, s_jsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.EmailAddress.Should().Be(export.EmailAddress);
        deserialized.InboxHash.Should().Be(export.InboxHash);
    }

    [Fact]
    public async Task ExportToStreamAsync_ShouldRespectCancellation()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        using var stream = new MemoryStream();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mockInbox.Object.ExportToStreamAsync(stream, cts.Token));
    }

    #endregion

    #region ExportToJsonAsync Tests

    [Fact]
    public async Task ExportToJsonAsync_ShouldReturnJsonString()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        // Act
        var json = await mockInbox.Object.ExportToJsonAsync();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test@example.com");
        json.Should().Contain("hash123abc");
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldProduceValidJson()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        // Act
        var json = await mockInbox.Object.ExportToJsonAsync();

        // Assert
        var deserialized = JsonSerializer.Deserialize<InboxExport>(json, s_jsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.EmailAddress.Should().Be(export.EmailAddress);
    }

    [Fact]
    public async Task ExportToJsonAsync_ShouldUseIndentedFormatting()
    {
        // Arrange
        var export = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(export);

        // Act
        var json = await mockInbox.Object.ExportToJsonAsync();

        // Assert
        json.Should().Contain("\n"); // Indented JSON contains newlines
    }

    #endregion

    #region ParseExportFromStreamAsync Tests

    [Fact]
    public async Task ParseExportFromStreamAsync_ShouldParseValidJson()
    {
        // Arrange
        var export = CreateTestExport();
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await InboxExportExtensions.ParseExportFromStreamAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.EmailAddress.Should().Be(export.EmailAddress);
        result.InboxHash.Should().Be(export.InboxHash);
        result.ServerSigPk.Should().Be(export.ServerSigPk);
    }

    [Fact]
    public async Task ParseExportFromStreamAsync_WithInvalidJson_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not valid json"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromStreamAsync(stream));

        ex.Message.Should().Contain("Failed to parse inbox export data");
    }

    [Fact]
    public async Task ParseExportFromStreamAsync_WithEmptyStream_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromStreamAsync(stream));
    }

    [Fact]
    public async Task ParseExportFromStreamAsync_WithNullJson_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromStreamAsync(stream));

        ex.Message.Should().Contain("null result");
    }

    [Fact]
    public async Task ParseExportFromStreamAsync_ShouldRespectCancellation()
    {
        // Arrange
        var export = CreateTestExport();
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => InboxExportExtensions.ParseExportFromStreamAsync(stream, cts.Token));
    }

    #endregion

    #region ParseExportFromJson Tests

    [Fact]
    public void ParseExportFromJson_ShouldParseValidJson()
    {
        // Arrange
        var export = CreateTestExport();
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Act
        var result = InboxExportExtensions.ParseExportFromJson(json);

        // Assert
        result.Should().NotBeNull();
        result.EmailAddress.Should().Be(export.EmailAddress);
        result.InboxHash.Should().Be(export.InboxHash);
        result.ServerSigPk.Should().Be(export.ServerSigPk);
    }

    [Fact]
    public void ParseExportFromJson_WithInvalidJson_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        var invalidJson = "not valid json";

        // Act & Assert
        var ex = Assert.Throws<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromJson(invalidJson));

        ex.Message.Should().Contain("Failed to parse inbox export data");
    }

    [Fact]
    public void ParseExportFromJson_WithNullJson_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        var nullJson = "null";

        // Act & Assert
        var ex = Assert.Throws<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromJson(nullJson));

        ex.Message.Should().Contain("null result");
    }

    [Fact]
    public void ParseExportFromJson_WithEmptyString_ShouldThrowInvalidImportDataException()
    {
        // Act & Assert
        Assert.Throws<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromJson(""));
    }

    [Fact]
    public void ParseExportFromJson_WithPartialJson_ShouldThrowInvalidImportDataException()
    {
        // Arrange
        var partialJson = "{\"emailAddress\": \"test@example.com\""; // Missing closing brace

        // Act & Assert
        Assert.Throws<InvalidImportDataException>(
            () => InboxExportExtensions.ParseExportFromJson(partialJson));
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public async Task ExportAndParse_ShouldRoundtripCorrectly()
    {
        // Arrange
        var originalExport = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(originalExport);

        // Act - Export to JSON
        var json = await mockInbox.Object.ExportToJsonAsync();

        // Act - Parse back
        var parsed = InboxExportExtensions.ParseExportFromJson(json);

        // Assert
        parsed.EmailAddress.Should().Be(originalExport.EmailAddress);
        parsed.InboxHash.Should().Be(originalExport.InboxHash);
        parsed.ServerSigPk.Should().Be(originalExport.ServerSigPk);
        parsed.PublicKeyB64.Should().Be(originalExport.PublicKeyB64);
        parsed.SecretKeyB64.Should().Be(originalExport.SecretKeyB64);
    }

    [Fact]
    public async Task ExportToStreamAndParse_ShouldRoundtripCorrectly()
    {
        // Arrange
        var originalExport = CreateTestExport();
        var mockInbox = new Mock<IInbox>();
        mockInbox.Setup(x => x.ExportAsync()).ReturnsAsync(originalExport);

        using var stream = new MemoryStream();

        // Act - Export to stream
        await mockInbox.Object.ExportToStreamAsync(stream);
        stream.Position = 0;

        // Act - Parse back
        var parsed = await InboxExportExtensions.ParseExportFromStreamAsync(stream);

        // Assert
        parsed.EmailAddress.Should().Be(originalExport.EmailAddress);
        parsed.InboxHash.Should().Be(originalExport.InboxHash);
        parsed.ServerSigPk.Should().Be(originalExport.ServerSigPk);
    }

    #endregion
}
