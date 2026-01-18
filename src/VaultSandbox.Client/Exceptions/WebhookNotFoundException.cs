namespace VaultSandbox.Client.Exceptions;

/// <summary>
/// Exception when a webhook is not found (404).
/// </summary>
public class WebhookNotFoundException : VaultSandboxException
{
    public string WebhookId { get; }

    public WebhookNotFoundException(string webhookId)
        : base($"Webhook not found: {webhookId}")
    {
        WebhookId = webhookId;
    }
}
