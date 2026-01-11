using System.Net.Sockets;
using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class SseExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "SSE connection failed";

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "SSE stream error";
        var innerException = new IOException("Network unreachable");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldAllowEmpty()
    {
        // Arrange
        const string message = "";

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void SseException_ShouldInheritFromVaultSandboxException()
    {
        // Arrange & Act
        var exception = new SseException("Test");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const string message = "SSE连接失败: 网络错误 📡";

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithNestedInnerException_ShouldPreserveChain()
    {
        // Arrange
        const string message = "SSE connection lost";
        var rootCause = new SocketException(10060);  // Connection timed out
        var innerException = new InvalidOperationException("Request failed", rootCause);

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootCause);
    }

    [Fact]
    public void Constructor_WithLongMessage_ShouldHandleLongContent()
    {
        // Arrange
        var message = "SSE error: " + new string('x', 10000);

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().StartWith("SSE error: ");
        exception.Message.Length.Should().Be(11 + 10000);
    }

    [Theory]
    [InlineData("Connection closed by server")]
    [InlineData("Failed to establish SSE connection")]
    [InlineData("SSE stream timed out")]
    [InlineData("Invalid SSE event format")]
    [InlineData("SSE reconnection limit exceeded")]
    [InlineData("Server sent unexpected event type")]
    public void Constructor_VariousErrorMessages_ShouldPreserveMessages(string message)
    {
        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithHttpRequestException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "SSE HTTP connection failed";
        var innerException = new HttpRequestException("Could not reach server");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public void Constructor_WithTaskCanceledException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "SSE connection was cancelled";
        var innerException = new TaskCanceledException("Operation was cancelled");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<TaskCanceledException>();
    }

    [Fact]
    public void Constructor_WithOperationCanceledException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "SSE operation cancelled";
        var innerException = new OperationCanceledException("Cancellation requested");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<OperationCanceledException>();
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldNotThrow()
    {
        // Arrange
        const string message = "SSE failed";

        // Act
        var exception = new SseException(message, null!);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_MessageWithNewlines_ShouldPreserveFormatting()
    {
        // Arrange
        const string message = "SSE connection failed\nReason: Server closed connection\nRetries: 3";

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().Contain("\n");
        exception.Message.Should().Contain("Retries: 3");
    }

    [Fact]
    public void Constructor_TimeoutScenario_ShouldCommunicateTimeout()
    {
        // Arrange
        const string message = "SSE connection timed out after 30 seconds";
        var innerException = new TimeoutException("The operation has timed out");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Contain("timed out");
        exception.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public void Constructor_ReconnectionScenario_ShouldHandleReconnectErrors()
    {
        // Arrange
        const string message = "SSE reconnection failed after maximum retry attempts (5)";

        // Act
        var exception = new SseException(message);

        // Assert
        exception.Message.Should().Contain("reconnection");
        exception.Message.Should().Contain("5");
    }

    [Fact]
    public void Constructor_WithIOException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "SSE stream read error";
        var innerException = new IOException("Unable to read data from the transport connection");

        // Act
        var exception = new SseException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<IOException>();
    }
}
