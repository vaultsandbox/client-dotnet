using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class InvalidImportDataExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Invalid import data format";

        // Act
        var exception = new InvalidImportDataException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAllowEmpty()
    {
        // Act
        var exception = new InvalidImportDataException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void InvalidImportDataException_ShouldInheritFromVaultSandboxException()
    {
        // Act
        var exception = new InvalidImportDataException("test");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData("Missing required field: email")]
    [InlineData("Invalid date format in column 3")]
    [InlineData("Duplicate entry found")]
    [InlineData("File exceeds maximum size limit")]
    public void Constructor_VariousMessages_ShouldHandleAll(string message)
    {
        // Act
        var exception = new InvalidImportDataException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const string message = "数据格式无效: 缺少必填字段";

        // Act
        var exception = new InvalidImportDataException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithLongMessage_ShouldHandleLongContent()
    {
        // Arrange
        var message = new string('x', 5000);

        // Act
        var exception = new InvalidImportDataException(message);

        // Assert
        exception.Message.Should().HaveLength(5000);
    }

    [Fact]
    public void InnerException_ShouldBeNull()
    {
        // Act
        var exception = new InvalidImportDataException("test");

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
