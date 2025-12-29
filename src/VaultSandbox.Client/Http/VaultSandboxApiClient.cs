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

        try
        {
            return await SendAsync<InboxSyncResponse>(
                HttpMethod.Get, $"/api/inboxes/{encodedEmail}/sync", ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new InboxNotFoundException(emailAddress);
        }
    }

    #endregion

    #region Email Operations

    public async Task<EmailResponse[]> GetEmailsAsync(string emailAddress, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);

        try
        {
            return await SendAsync<EmailResponse[]>(
                HttpMethod.Get, $"/api/inboxes/{encodedEmail}/emails", ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new InboxNotFoundException(emailAddress);
        }
    }

    public async Task<EmailResponse> GetEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        try
        {
            return await SendAsync<EmailResponse>(
                HttpMethod.Get, $"/api/inboxes/{encodedEmail}/emails/{encodedId}", ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new EmailNotFoundException(emailId);
        }
    }

    public async Task<RawEmailResponse> GetRawEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        try
        {
            return await SendAsync<RawEmailResponse>(
                HttpMethod.Get, $"/api/inboxes/{encodedEmail}/emails/{encodedId}/raw", ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new EmailNotFoundException(emailId);
        }
    }

    public async Task MarkEmailAsReadAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);

        try
        {
            await SendAsync(HttpMethod.Patch, $"/api/inboxes/{encodedEmail}/emails/{encodedId}/read", ct);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new EmailNotFoundException(emailId);
        }
    }

    public async Task DeleteEmailAsync(string emailAddress, string emailId, CancellationToken ct = default)
    {
        var encodedEmail = Uri.EscapeDataString(emailAddress);
        var encodedId = Uri.EscapeDataString(emailId);
        await SendAsync(HttpMethod.Delete, $"/api/inboxes/{encodedEmail}/emails/{encodedId}", ct);
    }

    #endregion

    #region SSE

    public async Task<Stream> GetEventsStreamAsync(IEnumerable<string> inboxHashes, CancellationToken ct = default)
    {
        var hashesParam = string.Join(",", inboxHashes);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/events?inboxes={hashesParam}");
        request.Headers.Accept.ParseAdd("text/event-stream");

        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadAsStreamAsync(ct);
    }

    #endregion

    #region Private Helpers

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
