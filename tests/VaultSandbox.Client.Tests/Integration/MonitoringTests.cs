using FluentAssertions;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for inbox monitoring functionality.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class MonitoringTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task MonitorDispose_ShouldStopReceivingEmails()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Monitor stop test {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var emailsReceived = new List<Email>();

        var monitor = Client.MonitorInboxes(inbox);
        var watchTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in monitor.WatchAsync())
                {
                    emailsReceived.Add(evt.Email);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when monitor is disposed
            }
        });

        // Give monitor time to start
        await Task.Delay(200);

        // Act - Dispose the monitor
        await monitor.DisposeAsync();

        // Wait for watch task to complete
        await Task.WhenAny(watchTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Send email after dispose (should not be received)
        await SmtpSender.SendEmailAsync(
            inbox.EmailAddress,
            subject,
            textBody: "This should not be received");

        await Task.Delay(500);

        // Assert - No emails should have been received after dispose
        emailsReceived.Should().BeEmpty("monitor was disposed before any emails were sent");
        watchTask.IsCompleted.Should().BeTrue("watch task should complete after dispose");
    }

    [SkippableFact]
    public async Task MonitorDispose_ShouldStopAllInboxWatching()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox1 = await Client.CreateInboxAsync();
        await using var inbox2 = await Client.CreateInboxAsync();
        var emailsReceived = new List<InboxEmailEvent>();

        var monitor = Client.MonitorInboxes(inbox1, inbox2);

        using var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in monitor.WatchAsync(cts.Token))
                {
                    emailsReceived.Add(evt);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        // Give monitor time to start
        await Task.Delay(200);

        // Act - Dispose the monitor (should stop watching all inboxes)
        await monitor.DisposeAsync();

        // Wait for watch task to complete
        await Task.WhenAny(watchTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        watchTask.IsCompleted.Should().BeTrue("disposing monitor should stop all watching");
    }

    [SkippableFact]
    public async Task MonitorDispose_MultipleTimes_ShouldBeIdempotent()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var monitor = Client.MonitorInboxes(inbox);

        // Act & Assert - Multiple dispose calls should not throw
        await monitor.DisposeAsync();
        await monitor.DisposeAsync();
        await monitor.DisposeAsync();
    }

    [SkippableFact]
    public async Task CancelWatching_ShouldStopReceivingCallbacks()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var subject = $"Cancel test {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var emailsReceived = new List<Email>();

        await using var monitor = Client.MonitorInboxes(inbox);
        using var cts = new CancellationTokenSource();

        var watchTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in monitor.WatchAsync(cts.Token))
                {
                    emailsReceived.Add(evt.Email);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        });

        // Give monitor time to start
        await Task.Delay(200);

        // Act - Cancel the watching
        await cts.CancelAsync();

        // Wait for task to complete
        await Task.WhenAny(watchTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Send email after cancellation
        await SmtpSender.SendEmailAsync(
            inbox.EmailAddress,
            subject,
            textBody: "This should not be received");

        await Task.Delay(500);

        // Assert
        emailsReceived.Should().BeEmpty("watching was cancelled before any emails were sent");
        watchTask.IsCompleted.Should().BeTrue("watch task should complete after cancellation");
    }
}
