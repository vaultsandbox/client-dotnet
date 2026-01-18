using FluentAssertions;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for inbox webhook operations.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class WebhookIntegrationTests : IntegrationTestBase
{
    private const string TestWebhookUrl = "https://webhook.site/test-endpoint";

    [SkippableFact]
    public async Task CreateWebhook_WithMinimalOptions_ShouldReturnWebhookWithSecret()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Assert
        webhook.Id.Should().StartWith("whk_");
        webhook.Url.Should().Be(TestWebhookUrl);
        webhook.Events.Should().ContainSingle(e => e == WebhookEventType.EmailReceived);
        webhook.Scope.Should().Be(WebhookScope.Inbox);
        webhook.InboxEmail.Should().Be(inbox.EmailAddress);
        webhook.Enabled.Should().BeTrue();
        webhook.Secret.Should().StartWith("whsec_");
        webhook.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [SkippableFact]
    public async Task CreateWebhook_WithAllOptions_ShouldReturnConfiguredWebhook()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var description = "Test webhook with all options";

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived, WebhookEventType.EmailDeleted],
            Description = description,
            Template = WebhookTemplateConfig.BuiltIn("simple"),
            Filter = new WebhookFilterConfig
            {
                Mode = FilterMode.All,
                RequireAuth = false,
                Rules =
                [
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.Subject,
                        Operator = FilterOperator.Contains,
                        Value = "important"
                    }
                ]
            }
        });

        // Assert
        webhook.Id.Should().StartWith("whk_");
        webhook.Events.Should().HaveCount(2);
        webhook.Events.Should().Contain(WebhookEventType.EmailReceived);
        webhook.Events.Should().Contain(WebhookEventType.EmailDeleted);
        webhook.Description.Should().Be(description);
        webhook.Template.Should().NotBeNull();
        webhook.Filter.Should().NotBeNull();
        webhook.Filter!.Mode.Should().Be(FilterMode.All);
        webhook.Filter.Rules.Should().HaveCount(1);
        webhook.Filter.Rules[0].Field.Should().Be(FilterableField.Subject);
        webhook.Filter.Rules[0].Operator.Should().Be(FilterOperator.Contains);
        webhook.Filter.Rules[0].Value.Should().Be("important");
    }

    [SkippableFact]
    public async Task CreateWebhook_WithCustomTemplate_ShouldReturnCustomTemplateConfig()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var customBody = "{\"from\": \"{{data.from.address}}\", \"subject\": \"{{data.subject}}\"}";

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived],
            Template = WebhookTemplateConfig.Custom(customBody, "application/json")
        });

        // Assert
        webhook.Id.Should().StartWith("whk_");
        webhook.Template.Should().NotBeNull();
        webhook.Template!.Custom.Should().NotBeNull();
        webhook.Template.Custom!.Body.Should().Be(customBody);
    }

    [SkippableFact]
    public async Task ListWebhooks_EmptyInbox_ShouldReturnEmptyList()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var webhooks = await inbox.ListWebhooksAsync();

        // Assert
        webhooks.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task ListWebhooks_WithMultipleWebhooks_ShouldReturnAll()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl + "/1",
            Events = [WebhookEventType.EmailReceived],
            Description = "Webhook 1"
        });

        await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl + "/2",
            Events = [WebhookEventType.EmailStored],
            Description = "Webhook 2"
        });

        await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl + "/3",
            Events = [WebhookEventType.EmailDeleted],
            Description = "Webhook 3"
        });

        // Act
        var webhooks = await inbox.ListWebhooksAsync();

        // Assert
        webhooks.Should().HaveCount(3);
        webhooks.Should().Contain(w => w.Description == "Webhook 1");
        webhooks.Should().Contain(w => w.Description == "Webhook 2");
        webhooks.Should().Contain(w => w.Description == "Webhook 3");

        // Secrets should NOT be included in list responses
        webhooks.Should().OnlyContain(w => w.Secret == null);
    }

    [SkippableFact]
    public async Task GetWebhook_ExistingWebhook_ShouldReturnWithSecretAndStats()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var created = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived],
            Description = "Test webhook"
        });

        // Act
        var webhook = await inbox.GetWebhookAsync(created.Id);

        // Assert
        webhook.Id.Should().Be(created.Id);
        webhook.Url.Should().Be(created.Url);
        webhook.Description.Should().Be(created.Description);
        webhook.Secret.Should().StartWith("whsec_");
        webhook.Stats.Should().NotBeNull();
        webhook.Stats!.TotalDeliveries.Should().BeGreaterThanOrEqualTo(0);
    }

    [SkippableFact]
    public async Task GetWebhook_NonExistentWebhook_ShouldThrowNotFoundException()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        Func<Task> act = () => inbox.GetWebhookAsync("whk_nonexistent12345");

        // Assert
        await act.Should().ThrowAsync<WebhookNotFoundException>();
    }

    [SkippableFact]
    public async Task UpdateWebhook_ChangeUrl_ShouldUpdateSuccessfully()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        var newUrl = TestWebhookUrl + "/updated";

        // Act
        var updated = await inbox.UpdateWebhookAsync(webhook.Id, new UpdateWebhookOptions
        {
            Url = newUrl
        });

        // Assert
        updated.Id.Should().Be(webhook.Id);
        updated.Url.Should().Be(newUrl);
        updated.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt.Should().BeAfter(webhook.CreatedAt);
    }

    [SkippableFact]
    public async Task UpdateWebhook_ChangeEvents_ShouldUpdateSuccessfully()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Act
        var updated = await inbox.UpdateWebhookAsync(webhook.Id, new UpdateWebhookOptions
        {
            Events = [WebhookEventType.EmailReceived, WebhookEventType.EmailStored, WebhookEventType.EmailDeleted]
        });

        // Assert
        updated.Events.Should().HaveCount(3);
        updated.Events.Should().Contain(WebhookEventType.EmailReceived);
        updated.Events.Should().Contain(WebhookEventType.EmailStored);
        updated.Events.Should().Contain(WebhookEventType.EmailDeleted);
    }

    [SkippableFact]
    public async Task UpdateWebhook_DisableWebhook_ShouldSetEnabledFalse()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        webhook.Enabled.Should().BeTrue();

        // Act
        var updated = await inbox.UpdateWebhookAsync(webhook.Id, new UpdateWebhookOptions
        {
            Enabled = false
        });

        // Assert
        updated.Enabled.Should().BeFalse();
    }

    [SkippableFact]
    public async Task UpdateWebhook_AddFilter_ShouldUpdateSuccessfully()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        webhook.Filter.Should().BeNull();

        // Act
        var options = new UpdateWebhookOptions
        {
            Filter = new WebhookFilterConfig
            {
                Mode = FilterMode.Any,
                Rules =
                [
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.FromAddress,
                        Operator = FilterOperator.Domain,
                        Value = "example.com"
                    }
                ]
            }
        };
        options.FilterWasSet = true;

        var updated = await inbox.UpdateWebhookAsync(webhook.Id, options);

        // Assert
        updated.Filter.Should().NotBeNull();
        updated.Filter!.Mode.Should().Be(FilterMode.Any);
        updated.Filter.Rules.Should().HaveCount(1);
        updated.Filter.Rules[0].Field.Should().Be(FilterableField.FromAddress);
        updated.Filter.Rules[0].Operator.Should().Be(FilterOperator.Domain);
    }

    [SkippableFact]
    public async Task UpdateWebhook_RemoveFilter_ShouldClearFilter()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived],
            Filter = new WebhookFilterConfig
            {
                Mode = FilterMode.All,
                Rules =
                [
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.Subject,
                        Operator = FilterOperator.Contains,
                        Value = "test"
                    }
                ]
            }
        });

        webhook.Filter.Should().NotBeNull();

        // Act
        var options = new UpdateWebhookOptions();
        options.RemoveFilter();

        var updated = await inbox.UpdateWebhookAsync(webhook.Id, options);

        // Assert
        updated.Filter.Should().BeNull();
    }

    [SkippableFact]
    public async Task DeleteWebhook_ExistingWebhook_ShouldRemoveWebhook()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Verify it exists
        var webhooks = await inbox.ListWebhooksAsync();
        webhooks.Should().Contain(w => w.Id == webhook.Id);

        // Act
        await inbox.DeleteWebhookAsync(webhook.Id);

        // Assert
        webhooks = await inbox.ListWebhooksAsync();
        webhooks.Should().NotContain(w => w.Id == webhook.Id);
    }

    [SkippableFact]
    public async Task DeleteWebhook_ViaWebhookObject_ShouldRemoveWebhook()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Act
        await webhook.DeleteAsync();

        // Assert
        var webhooks = await inbox.ListWebhooksAsync();
        webhooks.Should().NotContain(w => w.Id == webhook.Id);
    }

    [SkippableFact]
    public async Task DeleteWebhook_NonExistentWebhook_ShouldBeIdempotent()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act - Delete is idempotent, so deleting a non-existent webhook should not throw
        Func<Task> act = () => inbox.DeleteWebhookAsync("whk_nonexistent12345");

        // Assert - Should not throw (idempotent behavior)
        await act.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task TestWebhook_ShouldReturnTestResult()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Act
        var result = await inbox.TestWebhookAsync(webhook.Id);

        // Assert
        // The test may succeed or fail depending on the endpoint, but we should get a result
        result.Should().NotBeNull();
        // StatusCode and ResponseTime should be present if the endpoint responded
        if (result.Success)
        {
            result.StatusCode.Should().BeInRange(200, 299);
            result.ResponseTime.Should().BeGreaterThan(0);
        }
        else
        {
            result.Error.Should().NotBeNullOrEmpty();
        }
    }

    [SkippableFact]
    public async Task TestWebhook_ViaWebhookObject_ShouldReturnTestResult()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Act
        var result = await webhook.TestAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task RotateWebhookSecret_ShouldReturnNewSecret()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        var originalSecret = webhook.Secret;

        // Act
        var rotation = await inbox.RotateWebhookSecretAsync(webhook.Id);

        // Assert
        rotation.Id.Should().Be(webhook.Id);
        rotation.Secret.Should().StartWith("whsec_");
        rotation.Secret.Should().NotBe(originalSecret);
        rotation.PreviousSecretValidUntil.Should().BeAfter(DateTimeOffset.UtcNow);
        rotation.PreviousSecretValidUntil.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddHours(1),
            TimeSpan.FromMinutes(5));
    }

    [SkippableFact]
    public async Task RotateWebhookSecret_ViaWebhookObject_ShouldReturnNewSecret()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        var originalSecret = webhook.Secret;

        // Act
        var rotation = await webhook.RotateSecretAsync();

        // Assert
        rotation.Secret.Should().NotBe(originalSecret);
    }

    [SkippableFact]
    public async Task WebhookStats_ShouldTrackDeliveries()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Act
        var retrieved = await inbox.GetWebhookAsync(webhook.Id);

        // Assert
        retrieved.Stats.Should().NotBeNull();
        retrieved.Stats!.TotalDeliveries.Should().BeGreaterThanOrEqualTo(0);
        retrieved.Stats.SuccessfulDeliveries.Should().BeGreaterThanOrEqualTo(0);
        retrieved.Stats.FailedDeliveries.Should().BeGreaterThanOrEqualTo(0);
        retrieved.Stats.SuccessRate.Should().BeGreaterThanOrEqualTo(0);
    }

    [SkippableFact]
    public async Task CreateWebhook_WithAllEventTypes_ShouldSucceed()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [
                WebhookEventType.EmailReceived,
                WebhookEventType.EmailStored,
                WebhookEventType.EmailDeleted
            ]
        });

        // Assert
        webhook.Events.Should().HaveCount(3);
        webhook.Events.Should().Contain(WebhookEventType.EmailReceived);
        webhook.Events.Should().Contain(WebhookEventType.EmailStored);
        webhook.Events.Should().Contain(WebhookEventType.EmailDeleted);
    }

    [SkippableFact]
    public async Task CreateWebhook_WithMultipleFilterRules_ShouldSucceed()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived],
            Filter = new WebhookFilterConfig
            {
                Mode = FilterMode.All,
                RequireAuth = true,
                Rules =
                [
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.FromAddress,
                        Operator = FilterOperator.Domain,
                        Value = "trusted.com"
                    },
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.Subject,
                        Operator = FilterOperator.Contains,
                        Value = "urgent",
                        CaseSensitive = false
                    },
                    new WebhookFilterRuleConfig
                    {
                        Field = FilterableField.BodyText,
                        Operator = FilterOperator.Regex,
                        Value = "order.*confirmation"
                    }
                ]
            }
        });

        // Assert
        webhook.Filter.Should().NotBeNull();
        webhook.Filter!.Mode.Should().Be(FilterMode.All);
        webhook.Filter.RequireAuth.Should().BeTrue();
        webhook.Filter.Rules.Should().HaveCount(3);
    }

    [SkippableFact]
    public async Task CreateWebhook_WithBuiltInTemplates_ShouldSucceed()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();
        var templates = new[] { "slack", "discord", "teams", "simple", "notification", "zapier", "default" };

        foreach (var templateName in templates)
        {
            // Act
            var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
            {
                Url = $"{TestWebhookUrl}/{templateName}",
                Events = [WebhookEventType.EmailReceived],
                Template = WebhookTemplateConfig.BuiltIn(templateName)
            });

            // Assert
            webhook.Id.Should().StartWith("whk_");
            webhook.Template.Should().NotBeNull();

            // Cleanup for next iteration
            await inbox.DeleteWebhookAsync(webhook.Id);
        }
    }

    [SkippableFact]
    public async Task WebhookInboxEmail_ShouldMatchInboxAddress()
    {
        SkipIfNotConfigured();

        // Arrange
        await using var inbox = await Client.CreateInboxAsync();

        // Act
        var webhook = await inbox.CreateWebhookAsync(new CreateWebhookOptions
        {
            Url = TestWebhookUrl,
            Events = [WebhookEventType.EmailReceived]
        });

        // Assert
        webhook.InboxEmail.Should().Be(inbox.EmailAddress);
        webhook.InboxHash.Should().Be(inbox.InboxHash);
    }
}
