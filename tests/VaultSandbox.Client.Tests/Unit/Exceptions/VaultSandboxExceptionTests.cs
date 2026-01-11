using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class VaultSandboxExceptionTests
{
    [Fact]
    public void Constructor_Default_ShouldCreateException()
    {
        // Act
        var exception = new VaultSandboxException();

        // Assert
        exception.Message.Should().NotBeNullOrEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new VaultSandboxException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAllowEmpty()
    {
        // Act
        var exception = new VaultSandboxException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Outer error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new VaultSandboxException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldAllowNull()
    {
        // Arrange
        const string message = "Test message";

        // Act
        var exception = new VaultSandboxException(message, null!);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void VaultSandboxException_ShouldInheritFromException()
    {
        // Act
        var exception = new VaultSandboxException();

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithNestedInnerException_ShouldPreserveChain()
    {
        // Arrange
        const string message = "Outer error";
        var rootCause = new ArgumentException("Root cause");
        var innerException = new InvalidOperationException("Middle error", rootCause);

        // Act
        var exception = new VaultSandboxException(message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootCause);
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const string message = "Error: 无效操作 🚫";

        // Act
        var exception = new VaultSandboxException(message);

        // Assert
        exception.Message.Should().Be(message);
    }
}
