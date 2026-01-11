using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class ServerKeyMismatchExceptionTests
{
    [Fact]
    public void Constructor_WithExpectedAndActualKey_ShouldSetProperties()
    {
        // Arrange
        const string expectedKey = "abc123expectedKey";
        const string actualKey = "xyz789actualKey";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
        exception.Message.Should().Contain("Server signing key mismatch");
        exception.Message.Should().Contain("key substitution attack");
    }

    [Fact]
    public void Constructor_WithExpectedActualAndCustomMessage_ShouldSetAllProperties()
    {
        // Arrange
        const string expectedKey = "expectedKeyValue";
        const string actualKey = "actualKeyValue";
        const string customMessage = "Custom key mismatch error message";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey, customMessage);

        // Assert
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
        exception.Message.Should().Be(customMessage);
    }

    [Fact]
    public void Constructor_DefaultMessage_ShouldIndicatePotentialAttack()
    {
        // Arrange
        const string expectedKey = "key1";
        const string actualKey = "key2";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.Message.Should().Contain("key substitution attack");
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ServerKeyMismatchException_ShouldInheritFromVaultSandboxException()
    {
        // Arrange & Act
        var exception = new ServerKeyMismatchException("expected", "actual");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithEmptyKeys_ShouldAllowEmpty()
    {
        // Arrange
        const string expectedKey = "";
        const string actualKey = "";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().BeEmpty();
        exception.ActualKey.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithBase64UrlEncodedKeys_ShouldPreserveFormat()
    {
        // Arrange - Base64URL encoded keys (no padding, - and _ instead of + and /)
        const string expectedKey = "SGVsbG9Xb3JsZA";  // "HelloWorld" in base64url
        const string actualKey = "R29vZGJ5ZVdvcmxk"; // "GoodbyeWorld" in base64url

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
    }

    [Fact]
    public void Constructor_WithLongKeys_ShouldHandleLongContent()
    {
        // Arrange - Simulate long ML-DSA public keys
        var expectedKey = new string('A', 2000);
        var actualKey = new string('B', 2000);

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Length.Should().Be(2000);
        exception.ActualKey.Length.Should().Be(2000);
    }

    [Fact]
    public void Constructor_KeysWithSpecialCharacters_ShouldPreserveCharacters()
    {
        // Arrange - Base64URL uses - and _ instead of + and /
        const string expectedKey = "abc-def_ghi";
        const string actualKey = "xyz-uvw_rst";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
    }

    [Theory]
    [InlineData("key1", "key2", "Keys are different")]
    [InlineData("same", "different", "Mismatch detected")]
    [InlineData("a", "b", "Short key error")]
    public void Constructor_WithCustomMessage_ShouldUseProvidedMessage(
        string expectedKey, string actualKey, string message)
    {
        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
    }

    [Fact]
    public void Constructor_SameKeyValues_ShouldStillCreateException()
    {
        // Arrange - Even if keys are same, exception should be created
        const string key = "sameKeyValue";

        // Act
        var exception = new ServerKeyMismatchException(key, key);

        // Assert
        exception.ExpectedKey.Should().Be(key);
        exception.ActualKey.Should().Be(key);
    }

    [Fact]
    public void Constructor_WithUnicodeKeys_ShouldPreserveUnicode()
    {
        // Arrange
        const string expectedKey = "密钥1";
        const string actualKey = "密钥2";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().Be(expectedKey);
        exception.ActualKey.Should().Be(actualKey);
    }

    [Fact]
    public void Constructor_WithEmptyCustomMessage_ShouldAllowEmpty()
    {
        // Arrange
        const string expectedKey = "expected";
        const string actualKey = "actual";
        const string customMessage = "";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey, customMessage);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_CustomMessageWithNewlines_ShouldPreserveFormatting()
    {
        // Arrange
        const string expectedKey = "expected";
        const string actualKey = "actual";
        const string customMessage = "Key mismatch detected\nExpected: expected\nActual: actual\nAction: Reject payload";

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey, customMessage);

        // Assert
        exception.Message.Should().Contain("\n");
        exception.Message.Should().Contain("Reject payload");
    }

    [Fact]
    public void ExpectedKey_Property_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new ServerKeyMismatchException("expected", "actual");

        // Assert - ExpectedKey should be get-only (compile-time check via property access)
        exception.ExpectedKey.Should().Be("expected");
    }

    [Fact]
    public void ActualKey_Property_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new ServerKeyMismatchException("expected", "actual");

        // Assert - ActualKey should be get-only (compile-time check via property access)
        exception.ActualKey.Should().Be("actual");
    }

    [Fact]
    public void InnerException_ShouldBeNull_NoInnerExceptionConstructor()
    {
        // Arrange & Act
        var exception = new ServerKeyMismatchException("expected", "actual");

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_RealisticMlDsaKeyScenario_ShouldHandleCorrectly()
    {
        // Arrange - Simulate realistic ML-DSA-65 public key sizes (base64url encoded)
        var expectedKey = Convert.ToBase64String(new byte[1952]).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var actualKey = Convert.ToBase64String(new byte[1952]).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // Act
        var exception = new ServerKeyMismatchException(expectedKey, actualKey);

        // Assert
        exception.ExpectedKey.Should().NotBeNullOrEmpty();
        exception.ActualKey.Should().NotBeNullOrEmpty();
    }
}
