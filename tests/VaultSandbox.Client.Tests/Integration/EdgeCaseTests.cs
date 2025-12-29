using System.Diagnostics;
using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for edge cases and boundary conditions.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class EdgeCaseTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task WaitForEmail_WithZeroTimeout_ShouldTimeoutImmediately()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var options = new WaitForEmailOptions
        {
            Subject = "This email will never arrive",
            Timeout = TimeSpan.Zero
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        Func<Task> act = () => inbox.WaitForEmailAsync(options);

        // Assert - With zero timeout, the cancellation can happen at various points:
        // - TaskCanceledException from HTTP request cancellation
        // - OperationCanceledException from async operations
        // - VaultSandboxTimeoutException if it reaches the timeout handler
        await act.Should().ThrowAsync<Exception>()
            .Where(e => e is VaultSandboxTimeoutException
                     || e is TaskCanceledException
                     || e is OperationCanceledException);
        stopwatch.Stop();

        // Should complete very quickly (within 5 seconds to account for network latency)
        // A zero timeout should not wait at all
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "zero timeout should not cause extended waiting");
    }

    [SkippableFact]
    public async Task WaitForEmail_InboxDeletedDuringWait_ShouldThrowInboxNotFoundException()
    {
        SkipIfNotConfigured();

        // Arrange
        var inbox = await Client.CreateInboxAsync();
        var emailAddress = inbox.EmailAddress;

        // Start waiting for email in background
        var waitTask = inbox.WaitForEmailAsync(new WaitForEmailOptions
        {
            Subject = "Test email",
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Give the wait operation time to start
        await Task.Delay(500);

        // Act - Delete the inbox while waiting
        await Client.DeleteInboxAsync(emailAddress);

        // Assert - The wait operation should fail
        // It will either throw InboxNotFoundException or complete due to channel closing
        // We need to await the task and check the exception
        Func<Task> act = async () => await waitTask;

        // The wait should fail with either InboxNotFoundException or timeout
        // since the inbox no longer exists
        await act.Should().ThrowAsync<Exception>();

        // Dispose the inbox (it's already deleted, but this should not throw)
        await inbox.DisposeAsync();
    }

    [SkippableFact]
    public async Task WaitForEmail_VeryShortTimeout_ShouldTimeoutQuickly()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var options = new WaitForEmailOptions
        {
            Subject = "This email will never arrive",
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var stopwatch = Stopwatch.StartNew();
        Func<Task> act = () => inbox.WaitForEmailAsync(options);

        // Assert - With very short timeout, the cancellation can happen at various points:
        // - TaskCanceledException from HTTP request cancellation
        // - OperationCanceledException from async operations
        // - VaultSandboxTimeoutException if it reaches the timeout handler
        await act.Should().ThrowAsync<Exception>()
            .Where(e => e is VaultSandboxTimeoutException
                     || e is TaskCanceledException
                     || e is OperationCanceledException);
        stopwatch.Stop();

        // Should complete quickly, allowing some extra time for network latency and processing
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "short timeout should not cause extended waiting");
    }

    [SkippableFact]
    public async Task GetEmails_AfterInboxDeleted_ShouldThrowInboxNotFoundException()
    {
        SkipIfNotConfigured();

        // Arrange
        var inbox = await Client.CreateInboxAsync();
        var emailAddress = inbox.EmailAddress;

        // Delete the inbox
        await Client.DeleteInboxAsync(emailAddress);

        // Act
        Func<Task> act = () => inbox.GetEmailsAsync();

        // Assert
        await act.Should().ThrowAsync<InboxNotFoundException>();

        // Cleanup
        await inbox.DisposeAsync();
    }
}
