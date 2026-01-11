using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class SignatureVerificationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Signature verification failed";

        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Invalid signature";
        var innerException = new FormatException("Invalid base64 signature");

        // Act
        var exception = new SignatureVerificationException(message, innerException);

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
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void SignatureVerificationException_ShouldInheritFromVaultSandboxException()
    {
        // Arrange & Act
        var exception = new SignatureVerificationException("Test");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const string message = "签名验证失败: 数据可能被篡改 ⚠️";

        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithNestedInnerException_ShouldPreserveChain()
    {
        // Arrange
        const string message = "Signature verification failed";
        var rootCause = new ArgumentException("Invalid public key");
        var innerException = new InvalidOperationException("Key loading failed", rootCause);

        // Act
        var exception = new SignatureVerificationException(message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootCause);
    }

    [Fact]
    public void Constructor_WithLongMessage_ShouldHandleLongContent()
    {
        // Arrange
        var message = "Signature verification error: " + new string('x', 10000);

        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().StartWith("Signature verification error: ");
        exception.Message.Length.Should().Be(30 + 10000);
    }

    [Theory]
    [InlineData("Invalid signature format")]
    [InlineData("Signature does not match data")]
    [InlineData("Public key mismatch")]
    [InlineData("Signature timestamp expired")]
    [InlineData("Potential tampering detected")]
    public void Constructor_VariousErrorMessages_ShouldPreserveMessages(string message)
    {
        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithCryptographicException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "ML-DSA signature verification failed";
        var innerException = new System.Security.Cryptography.CryptographicException("Invalid signature");

        // Act
        var exception = new SignatureVerificationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<System.Security.Cryptography.CryptographicException>();
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldNotThrow()
    {
        // Arrange
        const string message = "Signature verification failed";

        // Act
        var exception = new SignatureVerificationException(message, null!);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_MessageWithNewlines_ShouldPreserveFormatting()
    {
        // Arrange
        const string message = "Signature verification failed\nExpected: abc123\nActual: def456";

        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().Contain("\n");
        exception.Message.Should().Contain("Expected: abc123");
    }

    [Fact]
    public void Constructor_TamperingScenario_ShouldCommunicateSeverity()
    {
        // Arrange
        const string message = "CRITICAL: Signature verification failed - potential data tampering detected";

        // Act
        var exception = new SignatureVerificationException(message);

        // Assert
        exception.Message.Should().Contain("CRITICAL");
        exception.Message.Should().Contain("tampering");
    }
}
