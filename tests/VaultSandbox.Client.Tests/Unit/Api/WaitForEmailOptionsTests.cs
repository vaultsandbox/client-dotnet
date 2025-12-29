using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class WaitForEmailOptionsTests
{
    [Fact]
    public void Matches_NoFilters_ShouldReturnTrue()
    {
        // Arrange
        var options = new WaitForEmailOptions();
        var email = CreateEmail();

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactSubject_ShouldReturnTrue()
    {
        // Arrange
        var options = new WaitForEmailOptions { Subject = "Welcome Email" };
        var email = CreateEmail(subject: "Welcome Email");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactSubject_ShouldBeCaseInsensitive()
    {
        // Arrange
        var options = new WaitForEmailOptions { Subject = "welcome email" };
        var email = CreateEmail(subject: "Welcome Email");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactSubject_ShouldReturnFalseWhenNoMatch()
    {
        // Arrange
        var options = new WaitForEmailOptions { Subject = "Different Subject" };
        var email = CreateEmail(subject: "Welcome Email");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_RegexSubject_ShouldReturnTrue()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = @"Verification code: \d+",
            UseRegex = true
        };
        var email = CreateEmail(subject: "Verification code: 123456");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_RegexSubject_ShouldReturnFalseWhenNoMatch()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = @"^Password reset",
            UseRegex = true
        };
        var email = CreateEmail(subject: "Verification code: 123456");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_ExactFrom_ShouldReturnTrue()
    {
        // Arrange
        var options = new WaitForEmailOptions { From = "noreply@example.com" };
        var email = CreateEmail(from: "noreply@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactFrom_ShouldBeCaseInsensitive()
    {
        // Arrange
        var options = new WaitForEmailOptions { From = "NoReply@Example.COM" };
        var email = CreateEmail(from: "noreply@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_ExactFrom_ShouldReturnFalseWhenNoMatch()
    {
        // Arrange
        var options = new WaitForEmailOptions { From = "different@example.com" };
        var email = CreateEmail(from: "noreply@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_RegexFrom_ShouldReturnTrue()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            From = @".*@example\.com$",
            UseRegex = true
        };
        var email = CreateEmail(from: "noreply@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_Predicate_ShouldApplyCustomFilter()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Predicate = e => e.Text?.Contains("secret-code") == true
        };
        var email = CreateEmail(text: "Your secret-code is 12345");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_Predicate_ShouldReturnFalseWhenPredicateFails()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Predicate = e => e.Text?.Contains("secret-code") == true
        };
        var email = CreateEmail(text: "No code here");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_MultipleFilters_ShouldRequireAllToMatch()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = "Welcome",
            From = "welcome@example.com"
        };
        var email = CreateEmail(subject: "Welcome", from: "welcome@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_MultipleFilters_ShouldReturnFalseIfAnyFails()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = "Welcome",
            From = "welcome@example.com"
        };
        var email = CreateEmail(subject: "Welcome", from: "different@example.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_SubjectFromAndPredicate_ShouldRequireAllToMatch()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = "Verification",
            From = "verify@example.com",
            Predicate = e => e.Text?.Contains("code") == true
        };
        var email = CreateEmail(
            subject: "Verification",
            from: "verify@example.com",
            text: "Your code is 123456");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_RegexWithSubjectAndFrom_ShouldApplyRegexToBoth()
    {
        // Arrange
        var options = new WaitForEmailOptions
        {
            Subject = @"Order #\d+",
            From = @"orders@.*\.com$",
            UseRegex = true
        };
        var email = CreateEmail(subject: "Order #12345", from: "orders@shop.com");

        // Act
        var result = options.Matches(email);

        // Assert
        result.Should().BeTrue();
    }

    private static Email CreateEmail(
        string subject = "Test Subject",
        string from = "test@example.com",
        string? text = null,
        string? html = null)
    {
        return new Email
        {
            Id = Guid.NewGuid().ToString(),
            InboxId = "inbox-hash-123",
            From = from,
            To = ["recipient@example.com"],
            Subject = subject,
            ReceivedAt = DateTimeOffset.UtcNow,
            Text = text,
            Html = html
        };
    }
}
