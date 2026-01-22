using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for inbox chaos configuration operations.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class ChaosIntegrationTests : IntegrationTestBase
{
    [SkippableFact]
    public async Task SetChaosConfig_WithLatency_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true,
                MinDelayMs = 1000,
                MaxDelayMs = 5000,
                Jitter = true,
                Probability = 0.5
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.Latency.Should().NotBeNull();
        config.Latency!.Enabled.Should().BeTrue();
        config.Latency.MinDelayMs.Should().Be(1000);
        config.Latency.MaxDelayMs.Should().Be(5000);
        config.Latency.Jitter.Should().BeTrue();
        config.Latency.Probability.Should().Be(0.5);
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithConnectionDrop_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            ConnectionDrop = new ConnectionDropOptions
            {
                Enabled = true,
                Probability = 0.3,
                Graceful = false
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.ConnectionDrop.Should().NotBeNull();
        config.ConnectionDrop!.Enabled.Should().BeTrue();
        config.ConnectionDrop.Probability.Should().Be(0.3);
        config.ConnectionDrop.Graceful.Should().BeFalse();
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithRandomError_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            RandomError = new RandomErrorOptions
            {
                Enabled = true,
                ErrorRate = 0.2,
                ErrorTypes = [RandomErrorType.Temporary]
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.RandomError.Should().NotBeNull();
        config.RandomError!.Enabled.Should().BeTrue();
        config.RandomError.ErrorRate.Should().Be(0.2);
        config.RandomError.ErrorTypes.Should().Contain(RandomErrorType.Temporary);
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithGreylist_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Greylist = new GreylistOptions
            {
                Enabled = true,
                RetryWindowMs = 600000,
                MaxAttempts = 3,
                TrackBy = GreylistTrackBy.IpSender
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.Greylist.Should().NotBeNull();
        config.Greylist!.Enabled.Should().BeTrue();
        config.Greylist.RetryWindowMs.Should().Be(600000);
        config.Greylist.MaxAttempts.Should().Be(3);
        config.Greylist.TrackBy.Should().Be(GreylistTrackBy.IpSender);
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithBlackhole_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Blackhole = new BlackholeOptions
            {
                Enabled = true,
                TriggerWebhooks = false
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.Blackhole.Should().NotBeNull();
        config.Blackhole!.Enabled.Should().BeTrue();
        config.Blackhole.TriggerWebhooks.Should().BeFalse();
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithExpiresAt_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            ExpiresAt = expiresAt,
            Latency = new LatencyOptions
            {
                Enabled = true
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.ExpiresAt.Should().NotBeNull();
        config.ExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(5));
    }

    [SkippableFact]
    public async Task SetChaosConfig_MultipleChaosTypes_ShouldReturnConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true,
                MinDelayMs = 500,
                MaxDelayMs = 3000,
                Probability = 0.8
            },
            RandomError = new RandomErrorOptions
            {
                Enabled = true,
                ErrorRate = 0.1,
                ErrorTypes = [RandomErrorType.Temporary, RandomErrorType.Permanent]
            }
        });

        // Assert
        config.Enabled.Should().BeTrue();
        config.Latency.Should().NotBeNull();
        config.Latency!.Enabled.Should().BeTrue();
        config.RandomError.Should().NotBeNull();
        config.RandomError!.Enabled.Should().BeTrue();
        config.RandomError.ErrorTypes.Should().HaveCount(2);
    }

    [SkippableFact]
    public async Task GetChaosConfig_AfterSetting_ShouldReturnSameConfiguration()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true,
                MinDelayMs = 2000,
                MaxDelayMs = 8000
            }
        });

        // Act
        var config = await inbox.GetChaosConfigAsync();

        // Assert
        config.Enabled.Should().BeTrue();
        config.Latency.Should().NotBeNull();
        config.Latency!.Enabled.Should().BeTrue();
        config.Latency.MinDelayMs.Should().Be(2000);
        config.Latency.MaxDelayMs.Should().Be(8000);
    }

    [SkippableFact]
    public async Task DisableChaos_AfterSetting_ShouldDisableChaos()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true
            }
        });

        // Act
        await inbox.DisableChaosAsync();

        // Assert
        var config = await inbox.GetChaosConfigAsync();
        config.Enabled.Should().BeFalse();
    }

    [SkippableFact]
    public async Task SetChaosConfig_DisabledViaEnabled_ShouldDisableChaos()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true
            }
        });

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = false
        });

        // Assert
        config.Enabled.Should().BeFalse();
    }

    [SkippableFact]
    public async Task CreateInbox_WithChaosConfig_ShouldEnableChaos()
    {
        SkipIfNotConfigured();

        // Arrange & Act
        await using var inbox = await Client.CreateInboxAsync(new CreateInboxOptions
        {
            Chaos = new SetChaosConfigOptions
            {
                Enabled = true,
                Blackhole = new BlackholeOptions
                {
                    Enabled = true
                }
            }
        });

        // Assert
        var config = await inbox.GetChaosConfigAsync();
        config.Enabled.Should().BeTrue();
        config.Blackhole.Should().NotBeNull();
        config.Blackhole!.Enabled.Should().BeTrue();
    }

    [SkippableFact]
    public async Task SetChaosConfig_WithDefaultValues_ShouldUseServerDefaults()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act - only set enabled=true for latency, let server use defaults
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Latency = new LatencyOptions
            {
                Enabled = true
            }
        });

        // Assert - server should fill in defaults
        config.Enabled.Should().BeTrue();
        config.Latency.Should().NotBeNull();
        config.Latency!.Enabled.Should().BeTrue();
        config.Latency.MinDelayMs.Should().Be(500); // default
        config.Latency.MaxDelayMs.Should().Be(10000); // default
        config.Latency.Jitter.Should().BeTrue(); // default
        config.Latency.Probability.Should().Be(1.0); // default
    }

    [SkippableFact]
    public async Task SetChaosConfig_GreylistTrackByIp_ShouldWork()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Greylist = new GreylistOptions
            {
                Enabled = true,
                TrackBy = GreylistTrackBy.Ip
            }
        });

        // Assert
        config.Greylist.Should().NotBeNull();
        config.Greylist!.TrackBy.Should().Be(GreylistTrackBy.Ip);
    }

    [SkippableFact]
    public async Task SetChaosConfig_GreylistTrackBySender_ShouldWork()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Greylist = new GreylistOptions
            {
                Enabled = true,
                TrackBy = GreylistTrackBy.Sender
            }
        });

        // Assert
        config.Greylist.Should().NotBeNull();
        config.Greylist!.TrackBy.Should().Be(GreylistTrackBy.Sender);
    }

    [SkippableFact]
    public async Task SetChaosConfig_RandomErrorPermanent_ShouldWork()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            RandomError = new RandomErrorOptions
            {
                Enabled = true,
                ErrorTypes = [RandomErrorType.Permanent]
            }
        });

        // Assert
        config.RandomError.Should().NotBeNull();
        config.RandomError!.ErrorTypes.Should().ContainSingle(t => t == RandomErrorType.Permanent);
    }

    [SkippableFact]
    public async Task SetChaosConfig_BlackholeWithWebhooks_ShouldWork()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var config = await inbox.SetChaosConfigAsync(new SetChaosConfigOptions
        {
            Enabled = true,
            Blackhole = new BlackholeOptions
            {
                Enabled = true,
                TriggerWebhooks = true
            }
        });

        // Assert
        config.Blackhole.Should().NotBeNull();
        config.Blackhole!.TriggerWebhooks.Should().BeTrue();
    }

    [SkippableFact]
    public async Task DisableChaos_MultipleTimesIsIdempotent()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act - disable multiple times
        await inbox.DisableChaosAsync();
        Func<Task> act = () => inbox.DisableChaosAsync();

        // Assert - should not throw
        await act.Should().NotThrowAsync();
    }
}
