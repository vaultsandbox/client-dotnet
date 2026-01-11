using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class EmailNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithEmailId_ShouldSetProperty()
    {
        // Arrange
        const string emailId = "abc123";

        // Act
        var exception = new EmailNotFoundException(emailId);

        // Assert
        exception.EmailId.Should().Be(emailId);
    }

    [Fact]
    public void Constructor_WithEmailId_ShouldSetFormattedMessage()
    {
        // Arrange
        const string emailId = "msg-456-xyz";

        // Act
        var exception = new EmailNotFoundException(emailId);

        // Assert
        exception.Message.Should().Be($"Email not found: {emailId}");
    }

    [Fact]
    public void Constructor_WithEmptyEmailId_ShouldAllowEmpty()
    {
        // Act
        var exception = new EmailNotFoundException(string.Empty);

        // Assert
        exception.EmailId.Should().BeEmpty();
        exception.Message.Should().Be("Email not found: ");
    }

    [Fact]
    public void EmailNotFoundException_ShouldInheritFromVaultSandboxException()
    {
        // Act
        var exception = new EmailNotFoundException("test-id");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("uuid-a1b2c3d4-e5f6")]
    [InlineData("msg_20231215_abcdef")]
    [InlineData("a")]
    public void Constructor_VariousEmailIdFormats_ShouldHandleAll(string emailId)
    {
        // Act
        var exception = new EmailNotFoundException(emailId);

        // Assert
        exception.EmailId.Should().Be(emailId);
        exception.Message.Should().Contain(emailId);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_ShouldPreserve()
    {
        // Arrange
        const string emailId = "msg-with-special!@#$%";

        // Act
        var exception = new EmailNotFoundException(emailId);

        // Assert
        exception.EmailId.Should().Be(emailId);
    }

    [Fact]
    public void InnerException_ShouldBeNull()
    {
        // Act
        var exception = new EmailNotFoundException("test-id");

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
