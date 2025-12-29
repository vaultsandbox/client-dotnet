using System.Runtime.CompilerServices;
using FluentAssertions;
using Moq;
using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Api;

public class InboxMonitorTests
{
    private static Email CreateTestEmail(string id, string subject) => new()
    {
        Id = id,
        InboxId = "test-inbox-hash",
        From = "sender@example.com",
        To = ["recipient@example.com"],
        Subject = subject,
        ReceivedAt = DateTimeOffset.UtcNow
    };

    private static Mock<IInbox> CreateMockInbox(string emailAddress, params Email[] emails)
    {
        var mock = new Mock<IInbox>();
        mock.Setup(x => x.EmailAddress).Returns(emailAddress);
        mock.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(emails));
        return mock;
    }

    private static async IAsyncEnumerable<Email> ToAsyncEnumerable(
        Email[] emails,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var email in emails)
        {
            if (ct.IsCancellationRequested) yield break;
            yield return email;
            await Task.Delay(1, ct); // Small delay to simulate async behavior
        }
    }

    [Fact]
    public void Constructor_SetsInboxesProperly()
    {
        // Arrange
        var inbox1 = CreateMockInbox("inbox1@test.com");
        var inbox2 = CreateMockInbox("inbox2@test.com");
        var inboxes = new[] { inbox1.Object, inbox2.Object };

        // Act
        var monitor = new InboxMonitor(inboxes);

        // Assert
        monitor.Inboxes.Should().HaveCount(2);
        monitor.InboxCount.Should().Be(2);
        monitor.Inboxes[0].EmailAddress.Should().Be("inbox1@test.com");
        monitor.Inboxes[1].EmailAddress.Should().Be("inbox2@test.com");
    }

    [Fact]
    public async Task WatchAsync_ReceivesEmailsFromMultipleInboxes()
    {
        // Arrange
        var email1 = CreateTestEmail("email1", "Subject 1");
        var email2 = CreateTestEmail("email2", "Subject 2");

        var inbox1 = CreateMockInbox("inbox1@test.com", email1);
        var inbox2 = CreateMockInbox("inbox2@test.com", email2);

        await using var monitor = new InboxMonitor([inbox1.Object, inbox2.Object]);
        var received = new List<InboxEmailEvent>();

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var evt in monitor.WatchAsync(cts.Token))
        {
            received.Add(evt);
            if (received.Count >= 2) break;
        }

        // Assert
        received.Should().HaveCount(2);
        received.Select(e => e.Email.Subject).Should().Contain("Subject 1");
        received.Select(e => e.Email.Subject).Should().Contain("Subject 2");
    }

    [Fact]
    public async Task WatchAsync_EventContainsCorrectInbox()
    {
        // Arrange
        var email = CreateTestEmail("email1", "Test Subject");
        var inbox = CreateMockInbox("test@example.com", email);

        await using var monitor = new InboxMonitor([inbox.Object]);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        InboxEmailEvent? receivedEvent = null;
        await foreach (var evt in monitor.WatchAsync(cts.Token))
        {
            receivedEvent = evt;
            break;
        }

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Inbox.Should().Be(inbox.Object);
        receivedEvent.InboxAddress.Should().Be("test@example.com");
        receivedEvent.Email.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public void Start_CanBeCalledExplicitly()
    {
        // Arrange
        var inbox = CreateMockInbox("test@example.com");
        var monitor = new InboxMonitor([inbox.Object]);

        // Act & Assert - Should not throw
        monitor.Start();
    }

    [Fact]
    public void Start_CanBeCalledMultipleTimes()
    {
        // Arrange
        var inbox = CreateMockInbox("test@example.com");
        var monitor = new InboxMonitor([inbox.Object]);

        // Act & Assert - Should not throw
        monitor.Start();
        monitor.Start();
        monitor.Start();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var inbox = CreateMockInbox("test@example.com");
        var monitor = new InboxMonitor([inbox.Object]);

        // Act & Assert - Should not throw
        await monitor.DisposeAsync();
        await monitor.DisposeAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CancelsWatching()
    {
        // Arrange
        var neverEndingInbox = new Mock<IInbox>();
        neverEndingInbox.Setup(x => x.EmailAddress).Returns("test@example.com");
        neverEndingInbox.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
            .Returns(NeverEndingStream);

        var monitor = new InboxMonitor([neverEndingInbox.Object]);
        monitor.Start();

        // Act
        await monitor.DisposeAsync();

        // Assert - Should complete without hanging
    }

    private static async IAsyncEnumerable<Email> NeverEndingStream(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(100, ct);
            yield return CreateTestEmail("email", "Test");
        }
    }

    [Fact]
    public async Task WatchAsync_RespectsExternalCancellation()
    {
        // Arrange
        var neverEndingInbox = new Mock<IInbox>();
        neverEndingInbox.Setup(x => x.EmailAddress).Returns("test@example.com");
        neverEndingInbox.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
            .Returns(NeverEndingStream);

        await using var monitor = new InboxMonitor([neverEndingInbox.Object]);
        var received = new List<InboxEmailEvent>();

        // Act
        using var cts = new CancellationTokenSource();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var evt in monitor.WatchAsync(cts.Token))
            {
                received.Add(evt);
                if (received.Count >= 2) await cts.CancelAsync();
            }
        });

        // Assert - Should complete without hanging
        await Task.WhenAny(watchTask, Task.Delay(TimeSpan.FromSeconds(5)));
        watchTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void InboxEmailEvent_PropertiesWorkCorrectly()
    {
        // Arrange
        var inbox = new Mock<IInbox>();
        inbox.Setup(x => x.EmailAddress).Returns("test@example.com");

        var email = CreateTestEmail("id123", "Test Subject");

        // Act
        var evt = new InboxEmailEvent(inbox.Object, email);

        // Assert
        evt.Inbox.Should().Be(inbox.Object);
        evt.Email.Should().Be(email);
        evt.InboxAddress.Should().Be("test@example.com");
    }

    [Fact]
    public void InboxEmailEvent_RecordEquality()
    {
        // Arrange
        var inbox = new Mock<IInbox>();
        inbox.Setup(x => x.EmailAddress).Returns("test@example.com");
        var email = CreateTestEmail("id123", "Test Subject");

        // Act
        var evt1 = new InboxEmailEvent(inbox.Object, email);
        var evt2 = new InboxEmailEvent(inbox.Object, email);

        // Assert - Records with same values should be equal
        evt1.Should().Be(evt2);
    }

    [Fact]
    public async Task WatchAsync_HandlesInboxDisposal()
    {
        // Arrange
        var inbox = new Mock<IInbox>();
        inbox.Setup(x => x.EmailAddress).Returns("test@example.com");
        inbox.Setup(x => x.WatchAsync(It.IsAny<CancellationToken>()))
            .Returns(ThrowsObjectDisposed);

        var monitor = new InboxMonitor([inbox.Object]);
        monitor.Start();

        // Wait briefly for the watch task to handle the ObjectDisposedException
        await Task.Delay(100);

        // Act & Assert - Disposing should still work cleanly
        await monitor.DisposeAsync();
    }

    private static async IAsyncEnumerable<Email> ThrowsObjectDisposed(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        throw new ObjectDisposedException("Inbox");
#pragma warning disable CS0162 // Unreachable code
        yield break;
#pragma warning restore CS0162
    }
}
