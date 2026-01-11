using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class VaultSandboxTimeoutExceptionTests
{
    [Fact]
    public void Constructor_WithTimeout_ShouldSetProperty()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Constructor_WithTimeout_ShouldSetFormattedMessage()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Message.Should().Be($"Operation timed out after {timeout.TotalMilliseconds}ms");
    }

    [Fact]
    public void Constructor_WithMessageAndTimeout_ShouldSetBoth()
    {
        // Arrange
        const string message = "Custom timeout message";
        var timeout = TimeSpan.FromMinutes(2);

        // Act
        var exception = new VaultSandboxTimeoutException(message, timeout);

        // Assert
        exception.Message.Should().Be(message);
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void VaultSandboxTimeoutException_ShouldInheritFromVaultSandboxException()
    {
        // Act
        var exception = new VaultSandboxTimeoutException(TimeSpan.FromSeconds(1));

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(60000)]
    public void Constructor_VariousTimeoutMilliseconds_ShouldHandleAll(int milliseconds)
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Timeout.Should().Be(timeout);
        exception.Message.Should().Contain(milliseconds.ToString());
    }

    [Fact]
    public void Constructor_WithZeroTimeout_ShouldHandle()
    {
        // Arrange
        var timeout = TimeSpan.Zero;

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Timeout.Should().Be(TimeSpan.Zero);
        exception.Message.Should().Contain("0ms");
    }

    [Fact]
    public void Constructor_WithLargeTimeout_ShouldHandle()
    {
        // Arrange
        var timeout = TimeSpan.FromHours(24);

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAllowEmpty()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var exception = new VaultSandboxTimeoutException(string.Empty, timeout);

        // Assert
        exception.Message.Should().BeEmpty();
        exception.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void Constructor_WithFractionalTimeout_ShouldPreservePrecision()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(1500.5);

        // Act
        var exception = new VaultSandboxTimeoutException(timeout);

        // Assert
        exception.Timeout.TotalMilliseconds.Should().BeApproximately(1500.5, 0.1);
    }

    [Fact]
    public void InnerException_ShouldBeNull()
    {
        // Act
        var exception = new VaultSandboxTimeoutException(TimeSpan.FromSeconds(5));

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithCustomMessageAndTimeout_InnerExceptionShouldBeNull()
    {
        // Act
        var exception = new VaultSandboxTimeoutException("Custom message", TimeSpan.FromSeconds(10));

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
