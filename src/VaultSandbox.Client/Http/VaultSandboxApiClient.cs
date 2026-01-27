using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Http;

/// <summary>
/// HTTP API client implementation with resilience handling.
/// </summary>
internal sealed class VaultSandboxApiClient : IVaultSandboxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VaultSandboxApiClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly bool _disposeHttpClient;

    public VaultSandboxApiClient(HttpClient httpClient, ILogger<VaultSandboxApiClient>? logger = null)
        : this(httpClient, disposeHttpClient: false, logger)
    {
    }

    public VaultSandboxApiClient(HttpClient httpClient, bool disposeHttpClient, ILogger<VaultSandboxApiClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = VaultSandboxJsonContext.Default
        };
    }

    #region Server Operations

    public async Task<CheckKeyResponse> CheckKeyAsync(CancellationToken ct = default)
    {
        return await SendAsync<CheckKeyResponse>(HttpMethod.Get, "/api/check-key", ct);
    }

    public async Task<ServerInfoResponse> GetServerInfoAsync(CancellationToken ct = default)
    {
        return await SendAsync<ServerInfoResponse>(HttpMethod.Get, "/api/server-info", ct);
    }

    #endregion

    #region Inbox Operations

    public async Task<CreateInboxResponse> CreateInboxAsync(CreateInboxRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CreateInboxResponse, CreateInboxRequest>(
            HttpMethod.Post, "/api/inboxes", request, ct);
    }

    public async Task DeleteInboxAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        await SendAsync(HttpMethod.Delete, $"/api/inboxes/{encodedEmail}", ct);
    }

    public async Task<DeleteAllInboxesResponse> DeleteAllInboxesAsync(CancellationToken ct = default)
    {
        return await SendAsync<DeleteAllInboxesResponse>(HttpMethod.Delete, "/api/inboxes", ct);
    }

    public async Task<InboxSyncResponse> GetInboxSyncAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        return await WithNotFoundHandling(
            () => SendAsync<InboxSyncResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/sync", ct),
            () => new InboxNotFoundException(emailAddress));
    }

    #endregion

    #region Email Operations

    public async Task<EmailResponse[]> GetEmailsAsync(string emailAddress, bool includeContent, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var url = $"/api/inboxes/{encodedEmail}/emails";
        if (includeContent)
        {
            url += "?includeContent=true";
        }

        return await WithNotFoundHandling(
            () => SendAsync<EmailResponse[]>(HttpMethod.Get, url, ct),
            () => new InboxNotFoundException(emailAddress));
    }

    public async Task<EmailResponse> GetEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        return await WithNotFoundHandling(
            () => SendAsync<EmailResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/emails/{encodedId}", ct),
            () => new EmailNotFoundException(emailId));
    }

    public async Task<RawEmailResponse> GetRawEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        return await WithNotFoundHandling(
            () => SendAsync<RawEmailResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/emails/{encodedId}/raw", ct),
            () => new EmailNotFoundException(emailId));
    }

    public async Task MarkEmailAsReadAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        await WithNotFoundHandling(
            () => SendAsync(HttpMethod.Patch, $"/api/inboxes/{encodedEmail}/emails/{encodedId}/read", ct),
            () => new EmailNotFoundException(emailId));
    }

    public async Task DeleteEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);
        await SendAsync(HttpMethod.Delete, $"/api/inboxes/{encodedEmail}/emails/{encodedId}", ct);
    }

    #endregion

    #region Inbox Webhooks

    public async Task<WebhookResponse> CreateInboxWebhookAsync(string emailAddress, CreateWebhookRequest request, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        return await WithNotFoundHandling(
            () => SendAsync<WebhookResponse, CreateWebhookRequest>(HttpMethod.Post, $"/api/inboxes/{encodedEmail}/webhooks", request, ct),
            () => new InboxNotFoundException(emailAddress));
    }

    public async Task<WebhookListResponse> ListInboxWebhooksAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        return await WithNotFoundHandling(
            () => SendAsync<WebhookListResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/webhooks", ct),
            () => new InboxNotFoundException(emailAddress));
    }

    public async Task<WebhookResponse> GetInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedWebhookId = Uri.EscapeDataString(webhookId);

        return await WithNotFoundHandling(
            () => SendAsync<WebhookResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/webhooks/{encodedWebhookId}", ct),
            () => new WebhookNotFoundException(webhookId));
    }

    public async Task<WebhookResponse> UpdateInboxWebhookAsync(string emailAddress, string webhookId, UpdateWebhookRequest request, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedWebhookId = Uri.EscapeDataString(webhookId);

        return await WithNotFoundHandling(
            () => SendAsync<WebhookResponse, UpdateWebhookRequest>(HttpMethod.Patch, $"/api/inboxes/{encodedEmail}/webhooks/{encodedWebhookId}", request, ct),
            () => new WebhookNotFoundException(webhookId));
    }

    public async Task DeleteInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedWebhookId = Uri.EscapeDataString(webhookId);

        await WithNotFoundHandling(
            () => SendAsync(HttpMethod.Delete, $"/api/inboxes/{encodedEmail}/webhooks/{encodedWebhookId}", ct),
            () => new WebhookNotFoundException(webhookId));
    }

    public async Task<TestWebhookResponse> TestInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedWebhookId = Uri.EscapeDataString(webhookId);

        return await WithNotFoundHandling(
            () => SendAsync<TestWebhookResponse>(HttpMethod.Post, $"/api/inboxes/{encodedEmail}/webhooks/{encodedWebhookId}/test", ct),
            () => new WebhookNotFoundException(webhookId));
    }

    public async Task<RotateSecretResponse> RotateInboxWebhookSecretAsync(string emailAddress, string webhookId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedWebhookId = Uri.EscapeDataString(webhookId);

        return await WithNotFoundHandling(
            () => SendAsync<RotateSecretResponse>(HttpMethod.Post, $"/api/inboxes/{encodedEmail}/webhooks/{encodedWebhookId}/rotate-secret", ct),
            () => new WebhookNotFoundException(webhookId));
    }

    #endregion

    #region Inbox Chaos Configuration

    public async Task<ChaosConfigResponse> GetInboxChaosConfigAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        return await WithNotFoundHandling(
            () => SendAsync<ChaosConfigResponse>(HttpMethod.Get, $"/api/inboxes/{encodedEmail}/chaos", ct),
            () => new InboxNotFoundException(emailAddress));
    }

    public async Task<ChaosConfigResponse> SetInboxChaosConfigAsync(string emailAddress, ChaosConfigRequest request, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        return await WithNotFoundHandling(
            () => SendAsync<ChaosConfigResponse, ChaosConfigRequest>(HttpMethod.Post, $"/api/inboxes/{encodedEmail}/chaos", request, ct),
            () => new InboxNotFoundException(emailAddress));
    }

    public async Task DeleteInboxChaosConfigAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        await WithNotFoundHandling(
            () => SendAsync(HttpMethod.Delete, $"/api/inboxes/{encodedEmail}/chaos", ct),
            () => new InboxNotFoundException(emailAddress));
    }

    #endregion

    #region SSE

    public async Task<HttpResponseMessage> GetEventsResponseAsync(IEnumerable<string> inboxHashes, CancellationToken ct = default)
    {
        var hashesParam = string.Join(",", inboxHashes);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/events?inboxes={hashesParam}");
        request.Headers.Accept.ParseAdd("text/event-stream");

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        await EnsureSuccessAsync(response, ct);

        return response;  // Caller owns and disposes
    }

    #endregion

    #region Private Helpers

    private async Task<T> WithNotFoundHandling<T>(
        Func<Task<T>> action,
        Func<Exception> createException)
    {
        try
        {
            return await action();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw createException();
        }
    }

    private async Task WithNotFoundHandling(
        Func<Task> action,
        Func<Exception> createException)
    {
        try
        {
            await action();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw createException();
        }
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        using var response = await _httpClient.SendAsync(request, ct);

        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, ct);
        return result ?? throw new ApiException((int)response.StatusCode, "Empty response body");
    }

    private async Task<TResponse> SendAsync<TResponse, TRequest>(
        HttpMethod method, string path, TRequest body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };

        using var response = await _httpClient.SendAsync(request, ct);

        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, ct);
        return result ?? throw new ApiException((int)response.StatusCode, "Empty response body");
    }

    private async Task SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var statusCode = (int)response.StatusCode;
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        _logger?.LogWarning(
            "API request failed with status {StatusCode}: {Body}",
            statusCode, responseBody);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ApiException(statusCode, "Invalid API key", responseBody),
            HttpStatusCode.Forbidden => new ApiException(statusCode, "Access forbidden", responseBody),
            HttpStatusCode.NotFound => new ApiException(statusCode, "Not found", responseBody),
            _ => new ApiException(statusCode, $"API request failed: {response.ReasonPhrase}", responseBody)
        };
    }

    #endregion

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
