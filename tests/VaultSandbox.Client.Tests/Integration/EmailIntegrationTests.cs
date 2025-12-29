using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for email operations using the full client with real SMTP.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class EmailIntegrationTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task GetEmails_EmptyInbox_ShouldReturnEmptyList()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var emails = await inbox.GetEmailsAsync();

        // Assert
        emails.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task SendAndReceiveEmail_SimpleText_ShouldDecryptSuccessfully()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Test Email {Guid.NewGuid():N}";
        var body = "Hello, this is a test email body!";

        // Act - Send email via SMTP
        await SendTestEmailAsync(inbox.EmailAddress, subject, body);

        // Wait for email
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Should().NotBeNull();
        email.Subject.Should().Be(subject);
        email.Text.Should().Contain(body);
        email.To.Should().Contain(inbox.EmailAddress);
    }

    [SkippableFact]
    public async Task SendAndReceiveEmail_WithFromAddress_ShouldMatchSender()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"From Test {Guid.NewGuid():N}";
        var fromAddress = "sender@test.example.com";

        // Act
        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body", from: fromAddress);

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.From.Should().Contain(fromAddress);
    }

    [SkippableFact]
    public async Task WaitForEmail_WithSubjectFilter_ShouldOnlyMatchSpecificEmail()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var targetSubject = $"TARGET-{Guid.NewGuid():N}";
        var otherSubject = $"OTHER-{Guid.NewGuid():N}";

        // Act - Send both emails
        await SendTestEmailAsync(inbox.EmailAddress, otherSubject, "This should not match");
        await SendTestEmailAsync(inbox.EmailAddress, targetSubject, "This should match");

        // Wait for specific email
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = targetSubject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Subject.Should().Be(targetSubject);
    }

    [SkippableFact]
    public async Task WaitForEmail_WithRegexFilter_ShouldMatchPattern()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var code = Random.Shared.Next(100000, 999999);
        var subject = $"Your verification code is {code}";

        // Act
        await SendTestEmailAsync(inbox.EmailAddress, subject, $"Code: {code}");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = @"verification code is \d+",
            UseRegex = true,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Subject.Should().Be(subject);
    }

    [SkippableFact]
    public async Task WaitForEmail_WithPredicate_ShouldApplyCustomFilter()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var secretToken = Guid.NewGuid().ToString("N");
        var subject = $"Email with token {Guid.NewGuid():N}";

        // Act
        await SendTestEmailAsync(inbox.EmailAddress, subject, $"Your secret token: {secretToken}");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Predicate = e => e.Text?.Contains(secretToken) == true,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Text.Should().Contain(secretToken);
    }

    [SkippableFact]
    public async Task GetEmails_AfterSendingMultiple_ShouldReturnAllEmails()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subjects = new[]
        {
            $"Email 1 - {Guid.NewGuid():N}",
            $"Email 2 - {Guid.NewGuid():N}",
            $"Email 3 - {Guid.NewGuid():N}"
        };

        // Act - Send multiple emails
        foreach (var subject in subjects)
        {
            await SendTestEmailAsync(inbox.EmailAddress, subject, $"Body for {subject}");
        }

        // Wait for all emails
        await Task.Delay(2000); // Give server time to process

        // Wait for the last email to ensure all are received
        await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subjects[^1],
            Timeout = TimeSpan.FromSeconds(30)
        });

        var emails = await inbox.GetEmailsAsync();

        // Assert
        emails.Should().HaveCountGreaterThanOrEqualTo(3);
        foreach (var subject in subjects)
        {
            emails.Should().Contain(e => e.Subject == subject);
        }
    }

    [SkippableFact]
    public async Task GetEmail_ById_ShouldReturnSpecificEmail()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"GetById Test {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body for GetById");

        var receivedEmail = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var emailById = await inbox.GetEmailAsync(receivedEmail.Id);

        // Assert
        emailById.Should().NotBeNull();
        emailById.Id.Should().Be(receivedEmail.Id);
        emailById.Subject.Should().Be(subject);
    }

    [SkippableFact]
    public async Task MarkAsRead_ShouldUpdateEmailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"MarkAsRead Test {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        email.IsRead.Should().BeFalse();

        // Act
        await inbox.MarkAsReadAsync(email.Id);

        // Assert
        var updatedEmail = await inbox.GetEmailAsync(email.Id);
        updatedEmail.IsRead.Should().BeTrue();
    }

    [SkippableFact]
    public async Task DeleteEmail_ShouldRemoveEmail()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"DeleteEmail Test {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        await inbox.DeleteEmailAsync(email.Id);

        // Assert
        Func<Task> act = () => inbox.GetEmailAsync(email.Id);
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }

    [SkippableFact]
    public async Task GetEmailCount_AfterSendingEmails_ShouldReturnCorrectCount()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var initialCount = await inbox.GetEmailCountAsync();

        var subject = $"Count Test {Guid.NewGuid():N}";
        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body");

        await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var newCount = await inbox.GetEmailCountAsync();

        // Assert
        newCount.Should().Be(initialCount + 1);
    }

    [SkippableFact]
    public async Task WatchAsync_ShouldReceiveNewEmails()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var receivedEmails = new List<Email>();
        var subject = $"Watch Test {Guid.NewGuid():N}";

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Start watching in background
        var watchTask = Task.Run(async () =>
        {
            await foreach (var email in inbox.WatchAsync(cts.Token))
            {
                receivedEmails.Add(email);
                if (email.Subject == subject)
                {
                    cts.Cancel(); // Stop watching after we get our email
                    break;
                }
            }
        });

        // Give watch a moment to start
        await Task.Delay(500);

        // Act - Send email while watching
        await SendTestEmailAsync(inbox.EmailAddress, subject, "Body for watch test");

        // Wait for watch to complete
        try
        {
            await watchTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        receivedEmails.Should().Contain(e => e.Subject == subject);
    }

    [SkippableFact]
    public async Task WaitForEmail_Timeout_ShouldThrowVaultSandboxTimeoutException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var options = new WaitForEmailOptions
        {
            Subject = "This subject will never arrive",
            Timeout = TimeSpan.FromSeconds(2)
        };

        // Act
        Func<Task> act = () => inbox.WaitForEmailAsync(options);

        // Assert
        await act.Should().ThrowAsync<VaultSandboxTimeoutException>();
    }

    [SkippableFact]
    public async Task WaitForEmail_WithCancellation_ShouldRespectToken()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        Func<Task> act = () => inbox.WaitForEmailAsync(
            new WaitForEmailOptions { Timeout = TimeSpan.FromMinutes(5) },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [SkippableFact]
    public async Task GetEmail_NonExistentId_ShouldThrowEmailNotFoundException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        Func<Task> act = () => inbox.GetEmailAsync("nonexistent-email-id");

        // Assert
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }

    [SkippableFact]
    public async Task GetEmailRaw_ShouldReturnRawMimeContent()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Raw Test {Guid.NewGuid():N}";
        var body = "Test body for raw email";

        await SendTestEmailAsync(inbox.EmailAddress, subject, body);

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act
        var rawContent = await inbox.GetEmailRawAsync(email.Id);

        // Assert
        rawContent.Should().NotBeNullOrEmpty();
        rawContent.Should().Contain(subject);
        rawContent.Should().Contain("MIME-Version:");
    }

    [SkippableFact]
    public async Task SendHtmlEmail_ShouldContainHtmlContent()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"HTML Test {Guid.NewGuid():N}";
        var htmlContent = "<html><body><h1>Test Header</h1><p>Paragraph content</p></body></html>";

        await SmtpSender.SendEmailAsync(
            inbox.EmailAddress,
            subject,
            textBody: "Plain text fallback",
            htmlBody: htmlContent);

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Html.Should().NotBeNullOrEmpty();
        email.Html.Should().Contain("<h1>");
    }

    [SkippableFact]
    public async Task SendEmailWithAttachment_ShouldIncludeAttachment()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Attachment Test {Guid.NewGuid():N}";
        var attachmentContent = System.Text.Encoding.UTF8.GetBytes("This is test file content.");

        await SmtpSender.SendEmailAsync(
            inbox.EmailAddress,
            subject,
            textBody: "Email with attachment",
            attachments: [("test.txt", attachmentContent, "text/plain")]);

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Attachments.Should().NotBeNullOrEmpty();
        email.Attachments.Should().Contain(a => a.Filename == "test.txt");
    }

    [SkippableFact]
    public async Task EmailMarkAsReadAsync_ShouldUpdateEmailStatus()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Email MarkAsRead Test {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body for email.MarkAsReadAsync");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        email.IsRead.Should().BeFalse();

        // Act - Use the Email instance method
        await email.MarkAsReadAsync();

        // Assert - The email's local IsRead property should be updated
        email.IsRead.Should().BeTrue();

        // Also verify by fetching the email again from the server
        var updatedEmail = await inbox.GetEmailAsync(email.Id);
        updatedEmail.IsRead.Should().BeTrue();
    }

    [SkippableFact]
    public async Task EmailDeleteAsync_ShouldRemoveEmail()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Email Delete Test {Guid.NewGuid():N}";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body for email.DeleteAsync");

        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Act - Use the Email instance method
        await email.DeleteAsync();

        // Assert - The email should no longer exist
        Func<Task> act = () => inbox.GetEmailAsync(email.Id);
        await act.Should().ThrowAsync<EmailNotFoundException>();
    }

    [SkippableFact]
    public async Task EmailHeaders_ShouldContainStandardHeaders()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Headers Test {Guid.NewGuid():N}";
        var fromAddress = "headers-test@example.com";

        await SendTestEmailAsync(inbox.EmailAddress, subject, "Test body for headers access", from: fromAddress);

        // Act
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = subject,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert
        email.Headers.Should().NotBeNull("email should have headers dictionary");
        email.Headers.Should().NotBeEmpty("headers dictionary should contain entries");

        // Standard email headers should be present
        // Note: Header names may be normalized/lowercase by the server
        var headerKeys = email.Headers!.Keys.Select(k => k.ToLowerInvariant()).ToList();

        // At minimum, we expect from, subject, and date-related headers
        headerKeys.Should().Contain(k => k.Contains("from") || k.Contains("sender"),
            "headers should contain from or sender information");
        headerKeys.Should().Contain(k => k.Contains("subject"),
            "headers should contain subject");
        headerKeys.Should().Contain(k => k.Contains("date") || k.Contains("received"),
            "headers should contain date or received timestamp");
    }

    [SkippableFact]
    public async Task WaitForEmail_WithFromRegexFilter_ShouldMatchSenderPattern()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var targetFrom = $"noreply-{uniqueId}@notifications.example.com";
        var otherFrom = $"support-{uniqueId}@help.example.com";
        var subject1 = $"Notification {Guid.NewGuid():N}";
        var subject2 = $"Support {Guid.NewGuid():N}";

        // Act - Send two emails from different senders
        await SendTestEmailAsync(inbox.EmailAddress, subject2, "From support", from: otherFrom);
        await SendTestEmailAsync(inbox.EmailAddress, subject1, "From notifications", from: targetFrom);

        // Use regex to match sender pattern: any email from notifications subdomain
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            From = @"noreply.*@notifications\.example\.com",
            UseRegex = true,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert - Should match the email from notifications.example.com
        email.From.Should().Contain(targetFrom);
        email.Subject.Should().Be(subject1);
    }

    [SkippableFact]
    public async Task WaitForEmail_WithFromExactMatch_ShouldMatchExactSender()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var targetFrom = $"specific-sender-{uniqueId}@test.example.com";
        var otherFrom = $"other-sender-{uniqueId}@test.example.com";
        var subject1 = $"From target {Guid.NewGuid():N}";
        var subject2 = $"From other {Guid.NewGuid():N}";

        // Act - Send two emails from different senders
        await SendTestEmailAsync(inbox.EmailAddress, subject2, "From other", from: otherFrom);
        await SendTestEmailAsync(inbox.EmailAddress, subject1, "From target", from: targetFrom);

        // Use exact match for sender address
        var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            From = targetFrom,
            UseRegex = false,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Assert - Should match the email from the exact sender
        email.From.Should().Contain(targetFrom);
        email.Subject.Should().Be(subject1);
    }
}
