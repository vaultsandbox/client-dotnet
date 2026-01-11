using FluentAssertions;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Http.Models;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class DeliveryStrategyBaseTests : IAsyncDisposable
{
    private readonly TestDeliveryStrategy _strategy;

    public DeliveryStrategyBaseTests()
    {
        _strategy = new TestDeliveryStrategy();
    }

    public async ValueTask DisposeAsync()
    {
        await _strategy.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAsync_SingleInbox_ShouldAddSubscription()
    {
        // Act
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5));

        // Assert
        _strategy.SubscriptionCount.Should().Be(1);
        _strategy.OnSubscribedCallCount.Should().Be(1);
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateInbox_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5));

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox-hash-1",
            "another@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Already subscribed*inbox-hash-1*");
    }

    [Fact]
    public async Task SubscribeAsync_MultipleInboxes_ShouldAddAllSubscriptions()
    {
        // Act
        await _strategy.SubscribeAsync("inbox-1", "a@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));
        await _strategy.SubscribeAsync("inbox-2", "b@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));
        await _strategy.SubscribeAsync("inbox-3", "c@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));

        // Assert
        _strategy.SubscriptionCount.Should().Be(3);
        _strategy.OnSubscribedCallCount.Should().Be(3);
    }

    [Fact]
    public async Task SubscribeAsync_WithReconnectCallback_ShouldStoreCallback()
    {
        // Act
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5),
            onReconnected: () => Task.CompletedTask);

        // Assert
        _strategy.SubscriptionCount.Should().Be(1);
        _strategy.HasReconnectCallback("inbox-hash-1").Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_WithoutReconnectCallback_ShouldWorkCorrectly()
    {
        // Act
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5),
            onReconnected: null);

        // Assert
        _strategy.SubscriptionCount.Should().Be(1);
        _strategy.HasReconnectCallback("inbox-hash-1").Should().BeFalse();
    }

    [Fact]
    public async Task UnsubscribeAsync_ExistingSubscription_ShouldRemoveSubscription()
    {
        // Arrange
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5));

        // Act
        await _strategy.UnsubscribeAsync("inbox-hash-1");

        // Assert
        _strategy.SubscriptionCount.Should().Be(0);
        _strategy.OnUnsubscribedCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UnsubscribeAsync_NonExistentSubscription_ShouldNotThrow()
    {
        // Act
        Func<Task> act = () => _strategy.UnsubscribeAsync("non-existent-inbox");

        // Assert
        await act.Should().NotThrowAsync();
        _strategy.OnUnsubscribedCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UnsubscribeAsync_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5));

        // Act
        await _strategy.UnsubscribeAsync("inbox-hash-1");
        await _strategy.UnsubscribeAsync("inbox-hash-1");
        await _strategy.UnsubscribeAsync("inbox-hash-1");

        // Assert
        _strategy.SubscriptionCount.Should().Be(0);
        _strategy.OnUnsubscribedCallCount.Should().Be(1); // Only called once
    }

    [Fact]
    public async Task UnsubscribeAsync_PartialUnsubscribe_ShouldOnlyRemoveSpecified()
    {
        // Arrange
        await _strategy.SubscribeAsync("inbox-1", "a@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));
        await _strategy.SubscribeAsync("inbox-2", "b@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));
        await _strategy.SubscribeAsync("inbox-3", "c@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));

        // Act
        await _strategy.UnsubscribeAsync("inbox-2");

        // Assert
        _strategy.SubscriptionCount.Should().Be(2);
        _strategy.HasSubscription("inbox-1").Should().BeTrue();
        _strategy.HasSubscription("inbox-2").Should().BeFalse();
        _strategy.HasSubscription("inbox-3").Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_WithCancellationToken_ShouldStoreToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5),
            ct: cts.Token);

        // Assert
        _strategy.SubscriptionCount.Should().Be(1);
    }

    [Fact]
    public async Task OnEmailCallback_ShouldBeInvokable()
    {
        // Arrange
        var receivedEvent = (SseEmailEvent?)null;
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            evt => { receivedEvent = evt; return Task.CompletedTask; },
            TimeSpan.FromSeconds(5));

        var testEvent = CreateTestSseEmailEvent("inbox-hash-1", "email-123");

        // Act
        await _strategy.SimulateEmailReceivedAsync("inbox-hash-1", testEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.InboxId.Should().Be("inbox-hash-1");
        receivedEvent.EmailId.Should().Be("email-123");
    }

    [Fact]
    public async Task OnEmailCallback_ErrorInCallback_ShouldNotAffectStrategy()
    {
        // Arrange
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => throw new InvalidOperationException("Callback error"),
            TimeSpan.FromSeconds(5));

        var testEvent = CreateTestSseEmailEvent("inbox-hash-1", "email-123");

        // Act
        Func<Task> act = () => _strategy.SimulateEmailReceivedAsync("inbox-hash-1", testEvent);

        // Assert - The callback exception should propagate but strategy should remain intact
        await act.Should().ThrowAsync<InvalidOperationException>();
        _strategy.SubscriptionCount.Should().Be(1);
    }

    [Fact]
    public async Task OnReconnectedCallback_ShouldBeInvokable()
    {
        // Arrange
        var reconnectCount = 0;
        await _strategy.SubscribeAsync(
            "inbox-hash-1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(5),
            onReconnected: () => { reconnectCount++; return Task.CompletedTask; });

        // Act
        await _strategy.SimulateReconnectAsync("inbox-hash-1");

        // Assert
        reconnectCount.Should().Be(1);
    }

    [Fact]
    public async Task SubscribeAndUnsubscribeAsync_ShouldInvokeVirtualMethods()
    {
        // Arrange & Act
        await _strategy.SubscribeAsync("inbox-1", "test@example.com", _ => Task.CompletedTask, TimeSpan.FromSeconds(5));
        await _strategy.UnsubscribeAsync("inbox-1");

        // Assert
        _strategy.OnSubscribedCallCount.Should().Be(1);
        _strategy.OnUnsubscribedCallCount.Should().Be(1);
    }

    private static SseEmailEvent CreateTestSseEmailEvent(string inboxId, string emailId)
    {
        return new SseEmailEvent
        {
            InboxId = inboxId,
            EmailId = emailId,
            EncryptedMetadata = new EncryptedPayload
            {
                Version = 1,
                Algorithms = new AlgorithmSuite
                {
                    Kem = "ML-KEM-768",
                    Sig = "ML-DSA-65",
                    Aead = "AES-256-GCM",
                    Kdf = "HKDF-SHA256"
                },
                CtKem = "dGVzdA",
                Nonce = "dGVzdG5vbmNl",
                Aad = "dGVzdGFhZA",
                Ciphertext = "dGVzdGNpcGhlcnRleHQ",
                Signature = "dGVzdHNpZw",
                ServerSigPk = "dGVzdHB1YmtleQ"
            }
        };
    }

    /// <summary>
    /// Test implementation of DeliveryStrategyBase to test base class functionality.
    /// </summary>
    private sealed class TestDeliveryStrategy : DeliveryStrategyBase
    {
        public override bool IsConnected => Subscriptions.Count > 0;

        public int OnSubscribedCallCount { get; private set; }
        public int OnUnsubscribedCallCount { get; private set; }
        public int SubscriptionCount => Subscriptions.Count;

        protected override Task OnSubscribedAsync(InboxSubscription subscription)
        {
            OnSubscribedCallCount++;
            return base.OnSubscribedAsync(subscription);
        }

        protected override Task OnUnsubscribedAsync(InboxSubscription subscription)
        {
            OnUnsubscribedCallCount++;
            return base.OnUnsubscribedAsync(subscription);
        }

        public bool HasSubscription(string inboxHash) => Subscriptions.ContainsKey(inboxHash);

        public bool HasReconnectCallback(string inboxHash) =>
            Subscriptions.TryGetValue(inboxHash, out var sub) && sub.OnReconnected != null;

        public async Task SimulateEmailReceivedAsync(string inboxHash, SseEmailEvent evt)
        {
            if (Subscriptions.TryGetValue(inboxHash, out var subscription))
            {
                await subscription.OnEmail(evt);
            }
        }

        public async Task SimulateReconnectAsync(string inboxHash)
        {
            if (Subscriptions.TryGetValue(inboxHash, out var subscription) && subscription.OnReconnected != null)
            {
                await subscription.OnReconnected();
            }
        }

        public override ValueTask DisposeAsync()
        {
            Subscriptions.Clear();
            return ValueTask.CompletedTask;
        }
    }
}
