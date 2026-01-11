using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class DecryptionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        const string message = "Failed to decrypt payload";

        // Act
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        const string message = "Decryption failed";
        var innerException = new ArgumentException("Invalid key");

        // Act
        var exception = new DecryptionException(message, innerException);

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
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void DecryptionException_ShouldInheritFromVaultSandboxException()
    {
        // Arrange & Act
        var exception = new DecryptionException("Test");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithUnicodeMessage_ShouldPreserveUnicode()
    {
        // Arrange
        const string message = "解密失败: 密钥无效 🔐";

        // Act
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithNestedInnerException_ShouldPreserveChain()
    {
        // Arrange
        const string message = "Decryption failed";
        var rootCause = new FormatException("Invalid format");
        var innerException = new InvalidOperationException("Key derivation failed", rootCause);

        // Act
        var exception = new DecryptionException(message, innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootCause);
    }

    [Fact]
    public void Constructor_WithLongMessage_ShouldHandleLongContent()
    {
        // Arrange
        var message = "Decryption error: " + new string('x', 10000);

        // Act
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().StartWith("Decryption error: ");
        exception.Message.Length.Should().Be(18 + 10000);
    }

    [Theory]
    [InlineData("Invalid ciphertext")]
    [InlineData("Key size mismatch")]
    [InlineData("Authentication tag verification failed")]
    [InlineData("Nonce reuse detected")]
    [InlineData("Corrupted encrypted data")]
    public void Constructor_VariousErrorMessages_ShouldPreserveMessages(string message)
    {
        // Act
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithCryptographicException_ShouldWrapCorrectly()
    {
        // Arrange
        const string message = "AES-GCM decryption failed";
        var innerException = new System.Security.Cryptography.CryptographicException("Bad data");

        // Act
        var exception = new DecryptionException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeOfType<System.Security.Cryptography.CryptographicException>();
    }

    [Fact]
    public void Constructor_WithNullInnerException_ShouldNotThrow()
    {
        // Arrange
        const string message = "Decryption failed";

        // Act
        var exception = new DecryptionException(message, null!);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_MessageWithNewlines_ShouldPreserveFormatting()
    {
        // Arrange
        const string message = "Decryption failed\nReason: Invalid key\nDetails: Key length mismatch";

        // Act
        var exception = new DecryptionException(message);

        // Assert
        exception.Message.Should().Contain("\n");
        exception.Message.Should().Contain("Reason: Invalid key");
    }
}
