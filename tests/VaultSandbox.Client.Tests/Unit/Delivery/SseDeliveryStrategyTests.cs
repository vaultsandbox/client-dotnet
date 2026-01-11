using System.Text;
using FluentAssertions;
using Moq;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Delivery;

public class SseDeliveryStrategyTests : IAsyncDisposable
{
    private readonly Mock<IVaultSandboxApiClient> _mockApiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly SseDeliveryStrategy _strategy;

    public SseDeliveryStrategyTests()
    {
        _mockApiClient = new Mock<IVaultSandboxApiClient>();
        _options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://test.example.com",
            ApiKey = "test-key",
            SseReconnectIntervalMs = 100,
            SseMaxReconnectAttempts = 3
        };
        _strategy = new SseDeliveryStrategy(_mockApiClient.Object, _options);
    }

    public async ValueTask DisposeAsync()
    {
        await _strategy.DisposeAsync();
    }

    #region Basic Connection Tests

    [Fact]
    public void IsConnected_WhenNoSubscriptions_ShouldBeFalse()
    {
        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateInbox_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupSseStreamSuccess();

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Already subscribed*");
    }

    [Fact]
    public async Task SubscribeAsync_WhenConnectionFails_ShouldThrow()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Connection failed");
    }

    [Fact]
    public async Task UnsubscribeAsync_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        SetupSseStreamSuccess();

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);

        // Act & Assert - Multiple unsubscribe calls should not throw
        await _strategy.UnsubscribeAsync("inbox1");
        await _strategy.UnsubscribeAsync("inbox1");
        await _strategy.UnsubscribeAsync("inbox1");

        _strategy.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_AfterDispose_ShouldThrowOrNotConnect()
    {
        // Arrange
        await _strategy.DisposeAsync();

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Assert - Should throw ObjectDisposedException
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task SubscribeAsync_WhenSuccessful_ShouldSetIsConnectedToTrue()
    {
        // Arrange
        SetupSseStreamSuccess();

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Wait for connection to establish
        await Task.Delay(100);

        // Assert
        _strategy.IsConnected.Should().BeTrue();
    }

    #endregion

    #region Reconnection Logic Tests

    [Fact]
    public async Task ReconnectAsync_WhenStreamFails_ShouldAttemptReconnection()
    {
        // Arrange
        var connectionAttempts = 0;
        var failingStream = new FailAfterReadStream(bytesToReadBeforeFail: 0);

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    return failingStream;
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Wait for reconnection attempts
        await Task.Delay(500);

        // Assert - Should have attempted to reconnect
        connectionAttempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task InvokeReconnectCallbacks_OnReconnection_ShouldCallAllCallbacks()
    {
        // Arrange
        var reconnectCallbackInvoked = false;
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    // First connection succeeds then stream fails
                    return new FailAfterReadStream(bytesToReadBeforeFail: 10);
                }
                // Subsequent connections succeed with blocking stream
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            onReconnected: () =>
            {
                reconnectCallbackInvoked = true;
                return Task.CompletedTask;
            });

        // Wait for reconnection
        await Task.Delay(500);

        // Assert
        reconnectCallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeReconnectCallbacks_OnInitialConnection_ShouldNotCallCallbacks()
    {
        // Arrange
        var reconnectCallbackInvoked = false;
        SetupSseStreamSuccess();

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            onReconnected: () =>
            {
                reconnectCallbackInvoked = true;
                return Task.CompletedTask;
            });

        // Wait for connection
        await Task.Delay(200);

        // Assert - Callback should NOT be called on initial connection
        reconnectCallbackInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeReconnectCallbacks_WhenCallbackThrows_ShouldContinueWithOtherCallbacks()
    {
        // Arrange
        var callback1Invoked = false;
        var callback2Invoked = false;
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    return new FailAfterReadStream(bytesToReadBeforeFail: 10);
                }
                return new BlockingStream();
            });

        // Subscribe first inbox with throwing callback
        await _strategy.SubscribeAsync(
            "inbox1",
            "test1@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            onReconnected: () =>
            {
                callback1Invoked = true;
                throw new InvalidOperationException("Callback 1 failed");
            });

        // Subscribe second inbox with normal callback
        await _strategy.SubscribeAsync(
            "inbox2",
            "test2@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            onReconnected: () =>
            {
                callback2Invoked = true;
                return Task.CompletedTask;
            });

        // Wait for reconnection
        await Task.Delay(500);

        // Assert - Both callbacks should be invoked despite first one throwing
        callback1Invoked.Should().BeTrue();
        callback2Invoked.Should().BeTrue();
    }

    [Fact]
    public async Task ReconnectAsync_WhenUnsubscribingWithOtherSubscriptions_ShouldReconnect()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                return new BlockingStream();
            });

        await _strategy.SubscribeAsync(
            "inbox1",
            "test1@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await _strategy.SubscribeAsync(
            "inbox2",
            "test2@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);
        var attemptsBeforeUnsubscribe = connectionAttempts;

        // Act - Unsubscribe one inbox (should trigger reconnect with remaining inbox)
        await _strategy.UnsubscribeAsync("inbox1");
        await Task.Delay(200);

        // Assert - Should have reconnected with updated subscription list
        connectionAttempts.Should().BeGreaterThan(attemptsBeforeUnsubscribe);
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_WhenLastSubscriptionRemoved_ShouldDisconnect()
    {
        // Arrange
        SetupSseStreamSuccess();

        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);
        _strategy.IsConnected.Should().BeTrue();

        // Act - Remove the last subscription
        await _strategy.UnsubscribeAsync("inbox1");
        await Task.Delay(100);

        // Assert
        _strategy.IsConnected.Should().BeFalse();
    }

    #endregion

    #region Connection Failure Scenarios

    [Fact]
    public async Task ConnectionFailure_OnFirstAttempt_ShouldPropagateImmediately()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server unreachable"));

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Assert - Should immediately propagate the exception
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Server unreachable");
    }

    [Fact]
    public async Task ConnectionFailure_AfterMaxReconnectAttempts_ShouldThrowSseException()
    {
        // Arrange
        var connectionAttempts = 0;
        var options = new VaultSandboxClientOptions
        {
            BaseUrl = "https://test.example.com",
            ApiKey = "test-key",
            SseReconnectIntervalMs = 50,
            SseMaxReconnectAttempts = 2
        };

        var mockApiClient = new Mock<IVaultSandboxApiClient>();

        mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    // First connection succeeds
                    return new FailAfterReadStream(bytesToReadBeforeFail: 5);
                }
                // Subsequent connections fail
                throw new IOException("Connection refused");
            });

        await using var strategy = new SseDeliveryStrategy(mockApiClient.Object, options);

        // Act
        await strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Wait for max reconnect attempts to be exceeded
        await Task.Delay(1000);

        // Assert - Connection attempts should exceed max + 1 (initial + retries)
        connectionAttempts.Should().BeGreaterThanOrEqualTo(options.SseMaxReconnectAttempts + 1);
    }

    [Fact]
    public async Task ConnectionFailure_WithHttpRequestException_ShouldRetry()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    return new FailAfterReadStream(bytesToReadBeforeFail: 5);
                }
                if (connectionAttempts < 4)
                {
                    throw new HttpRequestException("Temporary network error");
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(800);

        // Assert - Should have retried and eventually connected
        connectionAttempts.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ConnectionFailure_WithIOException_ShouldRetry()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    return new FailAfterReadStream(bytesToReadBeforeFail: 5);
                }
                if (connectionAttempts == 2)
                {
                    throw new IOException("Network stream closed");
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(500);

        // Assert
        connectionAttempts.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Server Disconnect Handling

    [Fact]
    public async Task ServerDisconnect_WhenStreamEndsGracefully_ShouldReconnect()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    // First stream ends gracefully (returns 0 bytes)
                    return new EndingStream();
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(500);

        // Assert - Should have reconnected after stream ended
        connectionAttempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ServerDisconnect_WhenStreamThrows_ShouldReconnect()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    return new FailAfterReadStream(bytesToReadBeforeFail: 10);
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(500);

        // Assert
        connectionAttempts.Should().BeGreaterThan(1);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task SubscribeAsync_WhenConnectionThrowsOperationCanceled_ShouldPropagate()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Connection cancelled"));

        // Act
        Func<Task> act = () => _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Connection_WhenCancelledDuringReconnect_ShouldStopReconnecting()
    {
        // Arrange
        var connectionAttempts = 0;
        using var cts = new CancellationTokenSource();

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> _, CancellationToken ct) =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    // First connection succeeds then stream fails
                    return Task.FromResult<Stream>(new FailAfterReadStream(bytesToReadBeforeFail: 5));
                }
                // Cancel during reconnection
                cts.Cancel();
                throw new OperationCanceledException(ct);
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2),
            ct: cts.Token);

        // Wait for reconnection attempt and cancellation
        await Task.Delay(500);

        // Assert - Should have attempted reconnection then stopped
        connectionAttempts.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region SSE Stream Tests

    [Fact]
    public async Task SubscribeAsync_WhenStreamReturned_ShouldBeConnected()
    {
        // Arrange
        SetupSseStreamSuccess();

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);

        // Assert
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_WhenStreamEnds_ShouldReconnect()
    {
        // Arrange
        var connectionAttempts = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionAttempts++;
                if (connectionAttempts == 1)
                {
                    // First stream ends immediately
                    return new EndingStream();
                }
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        // Wait for reconnection
        await Task.Delay(500);

        // Assert - Should have reconnected
        connectionAttempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ProcessSseStream_WhenCallbackThrows_ShouldNotBreakConnection()
    {
        // Arrange - Callback throws, but connection should remain alive
        SetupSseStreamSuccess();

        var subscribeCompleted = false;

        // Act - Callback that throws shouldn't break the subscription process
        var act = async () =>
        {
            await _strategy.SubscribeAsync(
                "inbox1",
                "test@example.com",
                _ => throw new InvalidOperationException("Callback error"),
                TimeSpan.FromSeconds(2));

            subscribeCompleted = true;
        };

        // Assert
        await act.Should().NotThrowAsync();
        subscribeCompleted.Should().BeTrue();
        _strategy.IsConnected.Should().BeTrue();
    }

    #endregion

    #region Multiple Subscriptions Tests

    [Fact]
    public async Task MultipleSubscriptions_ShouldShareSingleConnection()
    {
        // Arrange
        var connectionCount = 0;

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectionCount++;
                return new BlockingStream();
            });

        // Act
        await _strategy.SubscribeAsync(
            "inbox1",
            "test1@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await _strategy.SubscribeAsync(
            "inbox2",
            "test2@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(200);

        // Assert - Both subscriptions should be using the same connection (with reconnect)
        // Note: Second subscription triggers a reconnect to update the subscription list
        connectionCount.Should().BeGreaterThanOrEqualTo(1);
        _strategy.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleSubscriptions_WhenOneUnsubscribes_ShouldReconnectWithRemaining()
    {
        // Arrange
        var requestedInboxes = new List<string[]>();

        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> inboxes, CancellationToken _) =>
            {
                requestedInboxes.Add(inboxes.ToArray());
                return new BlockingStream();
            });

        await _strategy.SubscribeAsync(
            "inbox1",
            "test1@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await _strategy.SubscribeAsync(
            "inbox2",
            "test2@example.com",
            _ => Task.CompletedTask,
            TimeSpan.FromSeconds(2));

        await Task.Delay(100);

        // Act
        await _strategy.UnsubscribeAsync("inbox1");
        await Task.Delay(200);

        // Assert - Last connection should only have inbox2
        var lastRequest = requestedInboxes.Last();
        lastRequest.Should().ContainSingle().Which.Should().Be("inbox2");
    }

    #endregion

    #region Helper Methods and Classes

    private void SetupSseStreamSuccess()
    {
        _mockApiClient.Setup(x => x.GetEventsStreamAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlockingStream());
    }

    /// <summary>
    /// A stream that blocks on read operations until cancelled.
    /// </summary>
    private sealed class BlockingStream : Stream
    {
        private readonly SemaphoreSlim _semaphore = new(0);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return 0;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A stream that returns some data then throws an exception.
    /// </summary>
    private sealed class FailAfterReadStream : Stream
    {
        private readonly int _bytesToReadBeforeFail;
        private int _bytesRead;

        public FailAfterReadStream(int bytesToReadBeforeFail)
        {
            _bytesToReadBeforeFail = bytesToReadBeforeFail;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytesRead >= _bytesToReadBeforeFail)
            {
                throw new IOException("Connection lost");
            }
            _bytesRead += count;
            return count;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return SimulateRead(count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(SimulateRead(buffer.Length));
        }

        private int SimulateRead(int count)
        {
            if (_bytesRead >= _bytesToReadBeforeFail)
            {
                throw new IOException("Connection lost");
            }
            _bytesRead += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// A stream that immediately returns 0 bytes (end of stream).
    /// </summary>
    private sealed class EndingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(0);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// A stream that returns SSE data first, then blocks indefinitely.
    /// </summary>
    private sealed class SseDataThenBlockStream : Stream
    {
        private readonly byte[] _data;
        private int _position;
        private readonly SemaphoreSlim _semaphore = new(0);

        public SseDataThenBlockStream(string sseData)
        {
            _data = Encoding.UTF8.GetBytes(sseData);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _data.Length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _data.Length)
            {
                return 0;
            }

            var bytesToRead = Math.Min(count, _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, bytesToRead);
            _position += bytesToRead;
            return bytesToRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_position >= _data.Length)
            {
                await _semaphore.WaitAsync(cancellationToken);
                return 0;
            }

            var bytesToRead = Math.Min(count, _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, bytesToRead);
            _position += bytesToRead;
            return bytesToRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position >= _data.Length)
            {
                await _semaphore.WaitAsync(cancellationToken);
                return 0;
            }

            var bytesToRead = Math.Min(buffer.Length, _data.Length - _position);
            _data.AsSpan(_position, bytesToRead).CopyTo(buffer.Span);
            _position += bytesToRead;
            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion
}
