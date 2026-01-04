using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class InboxExportTests
{
    [Fact]
    public void InboxExport_ShouldContainAllRequiredFields()
    {
        // Arrange & Act
        var export = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            InboxHash = "hash123abc",
            ServerSigPk = "server-sig-pk-base64",
            SecretKey = "secret-key-base64",
            ExportedAt = DateTimeOffset.UtcNow
        };

        // Assert
        export.Version.Should().Be(1);
        export.EmailAddress.Should().Be("test@example.com");
        export.InboxHash.Should().Be("hash123abc");
        export.ServerSigPk.Should().Be("server-sig-pk-base64");
        export.SecretKey.Should().Be("secret-key-base64");
    }

    [Fact]
    public void InboxExport_Version_ShouldDefaultToOne()
    {
        // Arrange & Act
        var export = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = DateTimeOffset.UtcNow
        };

        // Assert - version defaults to 1 per spec
        export.Version.Should().Be(1);
    }

    [Fact]
    public void InboxExport_ShouldBeImmutableRecord()
    {
        // Arrange
        var original = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = DateTimeOffset.UtcNow
        };

        // Act
        var copy = original with { EmailAddress = "different@example.com" };

        // Assert
        original.EmailAddress.Should().Be("test@example.com");
        copy.EmailAddress.Should().Be("different@example.com");
    }

    [Fact]
    public void InboxExport_EqualityComparison_ShouldWorkCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var export1 = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = timestamp,
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = timestamp
        };

        var export2 = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = timestamp,
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = timestamp
        };

        // Assert
        export1.Should().Be(export2);
        export1.GetHashCode().Should().Be(export2.GetHashCode());
    }

    [Fact]
    public void InboxExport_ExpiresAt_ShouldBeInTheFuture()
    {
        // Arrange & Act
        var export = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = DateTimeOffset.UtcNow
        };

        // Assert
        export.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void InboxExport_ExportedAt_ShouldRecordCreationTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var export = new InboxExport
        {
            EmailAddress = "test@example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            InboxHash = "hash123",
            ServerSigPk = "server-pk",
            SecretKey = "secret-key",
            ExportedAt = DateTimeOffset.UtcNow
        };

        var after = DateTimeOffset.UtcNow;

        // Assert
        export.ExportedAt.Should().BeOnOrAfter(before);
        export.ExportedAt.Should().BeOnOrBefore(after);
    }
}
