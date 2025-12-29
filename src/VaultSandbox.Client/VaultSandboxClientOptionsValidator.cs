using Microsoft.Extensions.Options;

namespace VaultSandbox.Client;

/// <summary>
/// Validates VaultSandboxClientOptions at startup.
/// </summary>
internal sealed class VaultSandboxClientOptionsValidator
    : IValidateOptions<VaultSandboxClientOptions>
{
    public ValidateOptionsResult Validate(string? name, VaultSandboxClientOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            errors.Add("BaseUrl is required");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errors.Add("BaseUrl must be a valid HTTP(S) URL");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            errors.Add("ApiKey is required");
        }

        if (options.HttpTimeoutMs <= 0)
        {
            errors.Add("HttpTimeoutMs must be positive");
        }

        if (options.WaitTimeoutMs <= 0)
        {
            errors.Add("WaitTimeoutMs must be positive");
        }

        if (options.PollIntervalMs <= 0)
        {
            errors.Add("PollIntervalMs must be positive");
        }

        if (options.MaxRetries < 0)
        {
            errors.Add("MaxRetries cannot be negative");
        }

        if (options.RetryDelayMs <= 0)
        {
            errors.Add("RetryDelayMs must be positive");
        }

        if (options.SseReconnectIntervalMs <= 0)
        {
            errors.Add("SseReconnectIntervalMs must be positive");
        }

        if (options.SseMaxReconnectAttempts < 0)
        {
            errors.Add("SseMaxReconnectAttempts cannot be negative");
        }

        if (options.DefaultInboxTtlSeconds < 60)
        {
            errors.Add("DefaultInboxTtlSeconds must be at least 60 seconds");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
