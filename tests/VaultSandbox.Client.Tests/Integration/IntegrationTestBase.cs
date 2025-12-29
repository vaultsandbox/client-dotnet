using VaultSandbox.Client.Api;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Base class for integration tests requiring a live server and the full client.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IVaultSandboxClient Client { get; private set; } = null!;
    protected SmtpEmailSender SmtpSender { get; private set; } = null!;
    protected TestSettings Settings => TestConfiguration.Settings;

    public virtual async Task InitializeAsync()
    {
        if (!Settings.IsConfigured)
            return;

        Client = await VaultSandboxClientBuilder.Create()
            .WithBaseUrl(Settings.BaseUrl)
            .WithApiKey(Settings.ApiKey)
            .WithWaitTimeout(TimeSpan.FromSeconds(60))
            .WithPollInterval(TimeSpan.FromMilliseconds(500))
            .BuildAndValidateAsync();

        SmtpSender = new SmtpEmailSender(Settings.SmtpHost, Settings.SmtpPort);
    }

    public virtual async Task DisposeAsync()
    {
        SmtpSender?.Dispose();

        if (Client is null)
            return;

        try
        {
            await Client.DeleteAllInboxesAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }

        await Client.DisposeAsync();
    }

    protected void SkipIfNotConfigured()
    {
        Skip.IfNot(Settings.IsConfigured, "Integration tests require .env configuration");
    }

    protected async Task SendTestEmailAsync(
        string to,
        string subject,
        string body,
        string? from = null,
        CancellationToken ct = default)
    {
        // Small delay to ensure inbox is fully registered on the server
        await Task.Delay(100, ct);
        await SmtpSender.SendSimpleEmailAsync(to, subject, body, from, ct);
    }
}
