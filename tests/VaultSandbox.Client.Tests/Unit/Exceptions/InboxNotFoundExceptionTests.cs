using FluentAssertions;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Exceptions;

public class InboxNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithEmailAddress_ShouldSetProperty()
    {
        // Arrange
        const string emailAddress = "test@example.com";

        // Act
        var exception = new InboxNotFoundException(emailAddress);

        // Assert
        exception.EmailAddress.Should().Be(emailAddress);
    }

    [Fact]
    public void Constructor_WithEmailAddress_ShouldSetFormattedMessage()
    {
        // Arrange
        const string emailAddress = "user@domain.com";

        // Act
        var exception = new InboxNotFoundException(emailAddress);

        // Assert
        exception.Message.Should().Be($"Inbox not found: {emailAddress}");
    }

    [Fact]
    public void Constructor_WithEmptyEmailAddress_ShouldAllowEmpty()
    {
        // Act
        var exception = new InboxNotFoundException(string.Empty);

        // Assert
        exception.EmailAddress.Should().BeEmpty();
        exception.Message.Should().Be("Inbox not found: ");
    }

    [Fact]
    public void InboxNotFoundException_ShouldInheritFromVaultSandboxException()
    {
        // Act
        var exception = new InboxNotFoundException("test@example.com");

        // Assert
        exception.Should().BeAssignableTo<VaultSandboxException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Theory]
    [InlineData("simple@test.com")]
    [InlineData("user.name+tag@example.org")]
    [InlineData("test@subdomain.domain.co.uk")]
    [InlineData("a@b.c")]
    public void Constructor_VariousEmailFormats_ShouldHandleAll(string emailAddress)
    {
        // Act
        var exception = new InboxNotFoundException(emailAddress);

        // Assert
        exception.EmailAddress.Should().Be(emailAddress);
        exception.Message.Should().Contain(emailAddress);
    }

    [Fact]
    public void Constructor_WithUnicodeEmailAddress_ShouldPreserveUnicode()
    {
        // Arrange
        const string emailAddress = "用户@例子.测试";

        // Act
        var exception = new InboxNotFoundException(emailAddress);

        // Assert
        exception.EmailAddress.Should().Be(emailAddress);
        exception.Message.Should().Contain(emailAddress);
    }

    [Fact]
    public void InnerException_ShouldBeNull()
    {
        // Act
        var exception = new InboxNotFoundException("test@example.com");

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
