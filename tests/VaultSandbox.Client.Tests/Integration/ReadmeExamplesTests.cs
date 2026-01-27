// Documentation-driven tests that mirror the README examples.
//
// These tests exercise the same flows demonstrated in the README to keep
// examples from drifting from real behavior.

using System.Text;
using System.Text.Json;
using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Tests that validate the examples shown in the README.md file.
/// These ensure documentation stays in sync with actual behavior.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Category", "ReadmeExamples")]
public class ReadmeExamplesTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task QuickStart_Example()
    {
        SkipIfNotConfigured();

        // README: Quick Start
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var subject = $"README Quick Start {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                subject,
                textBody: "Plain text body from README quick start",
                htmlBody: "<p>HTML body from README quick start</p>",
                from: "quickstart@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Subject = "README Quick Start",
                UseRegex = true
            });

            // Email is already decrypted - just use it!
            email.From.Should().Contain("quickstart@example.com");
            email.Subject.Should().Be(subject);
            email.Text.Should().Contain("Plain text body");
            email.Html.Should().Contain("HTML body");
        }
    }

    [SkippableFact]
    public async Task PasswordResetEmails_Example()
    {
        SkipIfNotConfigured();

        // README: Testing Password Reset Emails
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var resetLink = $"https://app.example.com/reset-password?token={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                "Reset your password",
                textBody: $"Reset using {resetLink}",
                htmlBody: $"<a href=\"{resetLink}\">Reset password</a>",
                from: "support@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Subject = "Reset your password",
                UseRegex = true
            });

            // Extract reset link
            var discoveredResetLink = email.Links?.FirstOrDefault(url => url.Contains("/reset-password"));
            discoveredResetLink.Should().NotBeNull();
            discoveredResetLink.Should().Contain("https://");

            // Validate email authentication
            var authValidation = email.AuthResults?.Validate();
            // In a real test, this may not pass if the sender isn't fully configured.
            // A robust check verifies the validation was performed and has the correct shape.
            authValidation.Should().NotBeNull();
            authValidation!.Failures.Should().NotBeNull();
        }
    }

    [SkippableFact]
    public async Task EmailAuthentication_SpfDkimDmarc_Example()
    {
        SkipIfNotConfigured();

        // README: Testing Email Authentication (SPF/DKIM/DMARC)
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                "Auth example",
                textBody: "Auth results demo",
                from: "auth@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            var validation = email.AuthResults?.Validate();

            // We cannot guarantee a pass/fail outcome in all environments,
            // but the shape should align with the README.
            if (validation is not null)
            {
                // Verify the validation object has the expected shape
                validation.Failures.Should().NotBeNull();
            }

            // Or check individual results. Results can vary based on the sending source.
            if (email.AuthResults?.Spf is not null)
            {
                Enum.IsDefined(email.AuthResults.Spf.Result).Should().BeTrue();
            }
            if (email.AuthResults?.Dkim is not null)
            {
                email.AuthResults.Dkim.Count.Should().BeGreaterThan(0);
            }
            if (email.AuthResults?.Dmarc is not null)
            {
                Enum.IsDefined(email.AuthResults.Dmarc.Result).Should().BeTrue();
            }
        }
    }

    [SkippableFact]
    public async Task ExtractingAndValidatingLinks_Example()
    {
        SkipIfNotConfigured();

        // README: Extracting and Validating Links
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var verifyLink = $"https://app.example.com/verify?token={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                "Verify your email",
                textBody: $"Verify at {verifyLink}",
                htmlBody: $"<a href=\"{verifyLink}\">Verify</a>",
                from: "verify@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Subject = "Verify your email",
                UseRegex = true,
                Timeout = TimeSpan.FromSeconds(30)
            });

            // All links are automatically extracted
            var discoveredLink = email.Links?.FirstOrDefault(url => url.Contains("/verify"));
            discoveredLink.Should().NotBeNull();
            discoveredLink.Should().Contain("https://");
        }
    }

    [SkippableFact]
    public async Task TestingWithXUnit_Example()
    {
        SkipIfNotConfigured();

        // README: Testing with xUnit - should receive welcome email
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var subject = $"Welcome {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                subject,
                textBody: "Thank you for signing up",
                from: "noreply@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Subject = "Welcome",
                UseRegex = true
            });

            email.From.Should().Contain("noreply@example.com");
            email.Text.Should().Contain("Thank you for signing up");
        }
    }

    [SkippableFact]
    public async Task WaitingForMultipleEmails_Example()
    {
        SkipIfNotConfigured();

        // README: Waiting for Multiple Emails
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var emailCount = 3;

            // Send multiple emails
            for (var i = 0; i < emailCount; i++)
            {
                await SmtpSender.SendEmailAsync(
                    inbox.EmailAddress,
                    $"Notification {i + 1}",
                    textBody: $"Notification body {i + 1}",
                    from: "notify@example.com");
            }

            // Wait for all 3 emails to arrive
            await inbox.WaitForEmailCountAsync(emailCount, new WaitForEmailCountOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            // Now list and verify all emails
            var emails = await inbox.GetEmailsAsync();
            emails.Count.Should().Be(emailCount);
            emails[0].Subject.Should().Contain("Notification");
        }
    }

    [SkippableFact]
    public async Task RealTimeMonitoring_WatchAsync_Example()
    {
        SkipIfNotConfigured();

        // README: Real-time Monitoring with IAsyncEnumerable
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var subject = $"Watch Test {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            Email? receivedEmail = null;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Start watching in background
            var watchTask = Task.Run(async () =>
            {
                await foreach (var email in inbox.WatchAsync(cts.Token))
                {
                    if (email.Subject == subject)
                    {
                        receivedEmail = email;
                        await cts.CancelAsync();
                        break;
                    }
                }
            });

            // Give watch a moment to start
            await Task.Delay(500);

            // Send email while watching
            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                subject,
                textBody: "Watching for emails",
                from: "updates@example.com");

            try
            {
                await watchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when we cancel after receiving the email
            }

            receivedEmail.Should().NotBeNull();
            receivedEmail!.Subject.Should().Be(subject);
        }
    }

    [SkippableFact]
    public async Task MonitoringMultipleInboxes_Example()
    {
        SkipIfNotConfigured();

        // README: Monitoring Multiple Inboxes
        var inbox1 = await Client.CreateInboxAsync();
        var inbox2 = await Client.CreateInboxAsync();

        await using (inbox1)
        await using (inbox2)
        {
            var subject = $"Monitor inbox example {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            InboxEmailEvent? receivedEvent = null;

            var monitor = Client.MonitorInboxes(inbox1, inbox2);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var watchTask = Task.Run(async () =>
            {
                await foreach (var evt in monitor.WatchAsync(cts.Token))
                {
                    if (evt.Email.Subject == subject)
                    {
                        receivedEvent = evt;
                        await cts.CancelAsync();
                        break;
                    }
                }
            });

            // Give monitor a moment to start
            await Task.Delay(500);

            // Send to inbox2
            await SmtpSender.SendEmailAsync(
                inbox2.EmailAddress,
                subject,
                textBody: "Monitor body",
                from: "monitor@example.com");

            try
            {
                await watchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            receivedEvent.Should().NotBeNull();
            receivedEvent!.InboxAddress.Should().Be(inbox2.EmailAddress);
            receivedEvent.Email.Subject.Should().Be(subject);
        }
    }

    [SkippableFact]
    public async Task WaitOptions_Variations_Example()
    {
        SkipIfNotConfigured();

        // README: WaitOptions variations
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var resetSubject = $"Password Reset {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                "Ignore me",
                textBody: "first email",
                from: "reset@example.com");

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                resetSubject,
                textBody: "reset email body",
                from: "reset@example.com");

            // Filter by subject regex
            var emailBySubject = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Subject = "Password Reset",
                UseRegex = true
            });
            emailBySubject.Subject.Should().Be(resetSubject);

            // Filter by predicate
            var predicateSubject = $"Predicate Match {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                predicateSubject,
                textBody: "predicate body",
                from: "predicate@example.com");

            var emailByPredicate = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                Predicate = email => email.Subject == predicateSubject && email.To.Contains(inbox.EmailAddress)
            });
            emailByPredicate.Subject.Should().Be(predicateSubject);
        }
    }

    [SkippableFact]
    public async Task WorkingWithEmailAttachments_Example()
    {
        SkipIfNotConfigured();

        // README: Working with Email Attachments
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            // Create sample attachments
            var textFileContent = "This is a sample text file attachment.";
            var jsonFileContent = JsonSerializer.Serialize(
                new { message = "Hello from JSON", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                new JsonSerializerOptions { WriteIndented = true });

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                "Documents Attached",
                textBody: "Please find the attached documents.",
                attachments:
                [
                    ("readme.txt", Encoding.UTF8.GetBytes(textFileContent), "text/plain"),
                    ("data.json", Encoding.UTF8.GetBytes(jsonFileContent), "application/json")
                ],
                from: "attachments@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Subject = "Documents Attached",
                UseRegex = true,
                Timeout = TimeSpan.FromSeconds(30)
            });

            // Access attachments array
            email.Attachments.Should().NotBeNull();
            email.Attachments!.Count.Should().Be(2);

            // Check first attachment (text file)
            var textAttachment = email.Attachments.FirstOrDefault(att => att.Filename == "readme.txt");
            textAttachment.Should().NotBeNull();
            textAttachment!.ContentType.Should().Contain("text/plain");
            textAttachment.Size.Should().BeGreaterThan(0);

            // Decode and verify text file content
            if (textAttachment.Content is not null)
            {
                var decodedText = Encoding.UTF8.GetString(textAttachment.Content);
                decodedText.Should().Be(textFileContent);
            }

            // Check second attachment (JSON file)
            var jsonAttachment = email.Attachments.FirstOrDefault(att => att.Filename == "data.json");
            jsonAttachment.Should().NotBeNull();
            jsonAttachment!.ContentType.Should().Contain("application/json");
            jsonAttachment.Size.Should().BeGreaterThan(0);

            // Decode and parse JSON content
            if (jsonAttachment.Content is not null)
            {
                var decodedJson = Encoding.UTF8.GetString(jsonAttachment.Content);
                var parsedData = JsonSerializer.Deserialize<JsonElement>(decodedJson);
                parsedData.GetProperty("message").GetString().Should().Be("Hello from JSON");
                parsedData.GetProperty("timestamp").GetInt64().Should().BeGreaterThan(0);
            }
        }
    }

    [SkippableFact]
    public async Task ErrorHandling_SuccessPath_Example()
    {
        SkipIfNotConfigured();

        // README: Error Handling - successful email receipt
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            var subject = $"Error handling success {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            try
            {
                // Send email to ensure success path
                await SmtpSender.SendEmailAsync(
                    inbox.EmailAddress,
                    subject,
                    textBody: "Test body",
                    from: "errorhandling@example.com");

                // This should succeed
                var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
                {
                    Timeout = TimeSpan.FromSeconds(30)
                });

                email.Subject.Should().Be(subject);
            }
            catch (VaultSandboxTimeoutException)
            {
                throw new Exception("Should not timeout when email is sent");
            }
            catch (ApiException ex)
            {
                throw new Exception($"Unexpected API Error ({ex.StatusCode}): {ex.Message}");
            }
            catch (VaultSandboxException ex)
            {
                throw new Exception($"Unexpected SDK error: {ex.Message}");
            }
        }
    }

    [SkippableFact]
    public async Task ErrorHandling_TimeoutPath_Example()
    {
        SkipIfNotConfigured();

        // README: Error Handling - TimeoutError when no email arrives
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            VaultSandboxTimeoutException? caughtException = null;

            try
            {
                // Don't send email - this should timeout
                await inbox.WaitForEmailAsync(new WaitForEmailOptions
                {
                    Timeout = TimeSpan.FromSeconds(2)
                });

                throw new Exception("Should have thrown VaultSandboxTimeoutException");
            }
            catch (VaultSandboxTimeoutException ex)
            {
                // Expected - verify the error properties
                caughtException = ex;
                ex.Message.Should().NotBeNullOrEmpty();
                ex.Timeout.Should().Be(TimeSpan.FromSeconds(2));
            }
            catch (ApiException ex)
            {
                throw new Exception($"Unexpected API Error ({ex.StatusCode}): {ex.Message}");
            }
            catch (VaultSandboxException ex)
            {
                throw new Exception($"Unexpected SDK error: {ex.Message}");
            }

            // Verify we actually caught a VaultSandboxTimeoutException
            caughtException.Should().NotBeNull();
            caughtException.Should().BeOfType<VaultSandboxTimeoutException>();
        }
    }

    [SkippableFact]
    public async Task Webhooks_Example()
    {
        SkipIfNotConfigured();

        // README: Webhooks
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            // Create a webhook for email.received events
            var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
            {
                Url = "https://httpbin.org/post",
                Events = [WebhookEventType.EmailReceived],
                Description = "Test webhook for README example"
            });

            webhook.Id.Should().StartWith("whk_");
            webhook.Secret.Should().StartWith("whsec_");
            webhook.Events.Should().Contain(WebhookEventType.EmailReceived);
            webhook.Description.Should().Be("Test webhook for README example");

            // Test the webhook
            var testResult = await webhook.TestAsync();
            // httpbin.org should respond successfully
            testResult.Success.Should().BeTrue();
            testResult.StatusCode.Should().Be(200);

            // List all webhooks
            var webhooks = await inbox.ListWebhooksAsync();
            webhooks.Count.Should().BeGreaterThanOrEqualTo(1);
            webhooks.Should().Contain(w => w.Id == webhook.Id);

            // Clean up
            await webhook.DeleteAsync();

            // Verify deletion
            var remainingWebhooks = await inbox.ListWebhooksAsync();
            remainingWebhooks.Should().NotContain(w => w.Id == webhook.Id);
        }
    }

    [SkippableFact]
    public async Task ChaosEngineering_Example()
    {
        SkipIfNotConfigured();

        // README: Chaos Engineering
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            try
            {
                // Enable latency injection
                var chaosConfig = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
                {
                    Enabled = true,
                    Latency = new LatencyOptions
                    {
                        Enabled = true,
                        MinDelayMs = 100,
                        MaxDelayMs = 500,
                        Probability = 1.0
                    }
                });

                chaosConfig.Enabled.Should().BeTrue();
                chaosConfig.Latency.Should().NotBeNull();
                chaosConfig.Latency!.Enabled.Should().BeTrue();
                chaosConfig.Latency.MinDelayMs.Should().Be(100);
                chaosConfig.Latency.MaxDelayMs.Should().Be(500);

                // Verify chaos is configured
                var fetchedConfig = await inbox.GetChaosConfigAsync();
                fetchedConfig.Enabled.Should().BeTrue();
                fetchedConfig.Latency?.Enabled.Should().BeTrue();

                // Disable chaos when done
                await inbox.DisableChaosAsync();

                // Verify chaos is disabled
                var disabledConfig = await inbox.GetChaosConfigAsync();
                disabledConfig.Enabled.Should().BeFalse();
            }
            catch (ApiException ex) when (ex.StatusCode == 403)
            {
                // Chaos may be disabled globally on the server - skip this test
                Skip.If(true, "Chaos engineering is disabled on this server");
            }
        }
    }

    #region Authentication Results Explicit Assertions

    [SkippableFact]
    public async Task DirectSend_FailsSpf_Example()
    {
        SkipIfNotConfigured();

        // When sending email directly from a test SMTP server that is not in the
        // domain's SPF record, SPF should fail or not pass.
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            // Use a domain that definitely doesn't authorize our test server's IP
            var spoofedFrom = "ceo@microsoft.com";

            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                $"SPF Test {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                textBody: "This email is sent from an unauthorized IP for the sender domain.",
                from: spoofedFrom);

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            // SPF should NOT pass when sending from an unauthorized IP
            email.AuthResults.Should().NotBeNull("email should have authentication results");

            if (email.AuthResults!.Spf is not null)
            {
                // SPF result should not be Pass for a spoofed domain
                email.AuthResults.Spf.Result.Should().NotBe(SpfStatus.Pass,
                    "SPF should fail when sending from an IP not in the domain's SPF record");
            }

            var validation = email.AuthResults.Validate();
            validation.SpfPassed.Should().BeFalse(
                "SPF validation should fail for emails sent from unauthorized IPs");
        }
    }

    [SkippableFact]
    public async Task DirectSend_FailsDkim_Example()
    {
        SkipIfNotConfigured();

        // When sending email directly from a test SMTP server without DKIM signing,
        // DKIM should fail or not pass.
        var inbox = await Client.CreateInboxAsync();
        await using (inbox)
        {
            // Send without any DKIM signature (our test SMTP server doesn't sign)
            await SmtpSender.SendEmailAsync(
                inbox.EmailAddress,
                $"DKIM Test {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                textBody: "This email has no DKIM signature.",
                from: "unsigned@example.com");

            var email = await inbox.WaitForEmailAsync(new WaitForEmailOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            });

            // Check DKIM status
            email.AuthResults.Should().NotBeNull("email should have authentication results");

            var validation = email.AuthResults!.Validate();

            // DKIM should NOT pass when there's no valid DKIM signature
            // (either no signatures at all, or signatures that don't verify)
            validation.DkimPassed.Should().BeFalse(
                "DKIM should not pass when email is sent without a valid DKIM signature");

            // If DKIM results are present, none should be Pass
            if (email.AuthResults.Dkim is { Count: > 0 })
            {
                email.AuthResults.Dkim.Should().NotContain(d => d.Result == DkimStatus.Pass,
                    "No DKIM signature should pass for unsigned emails");
            }
        }
    }

    #endregion
}
