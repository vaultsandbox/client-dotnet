namespace VaultSandbox.Client.Http;

/// <summary>
/// HTTP message handler that adds the X-API-Key header to all requests.
/// </summary>
internal sealed class ApiKeyAuthenticationHandler : DelegatingHandler
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly string _apiKey;

    public ApiKeyAuthenticationHandler(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
