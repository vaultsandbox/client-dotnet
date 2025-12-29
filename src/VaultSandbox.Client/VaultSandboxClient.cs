using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Diagnostics;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client;

/// <summary>
/// Main entry point for the VaultSandbox client SDK.
/// </summary>
public sealed class VaultSandboxClient : IVaultSandboxClient
{
    /// <summary>
    /// Expected ML-KEM-768 public key size in bytes.
    /// </summary>
    internal const int MlKemPublicKeySize = 1184;

    /// <summary>
    /// Expected ML-KEM-768 secret key size in bytes.
    /// </summary>
    internal const int MlKemSecretKeySize = 2400;

    private readonly IVaultSandboxApiClient _apiClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DeliveryStrategyFactory _deliveryStrategyFactory;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<VaultSandboxClient>? _logger;
    private readonly bool _disposeApiClient;

    private readonly ConcurrentDictionary<string, IDeliveryStrategy> _strategies = new();
    private bool _isDisposed;

    internal VaultSandboxClient(
        IVaultSandboxApiClient apiClient,
        ICryptoProvider cryptoProvider,
        VaultSandboxClientOptions options,
        ILoggerFactory? loggerFactory = null)
        : this(apiClient, cryptoProvider, options, disposeApiClient: false, loggerFactory)
    {
    }

    internal VaultSandboxClient(
        IVaultSandboxApiClient apiClient,
        ICryptoProvider cryptoProvider,
        VaultSandboxClientOptions options,
        bool disposeApiClient,
        ILoggerFactory? loggerFactory = null)
    {
        _apiClient = apiClient;
        _cryptoProvider = cryptoProvider;
        _options = options;
        _disposeApiClient = disposeApiClient;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<VaultSandboxClient>();

        _deliveryStrategyFactory = new DeliveryStrategyFactory(
            apiClient, options, loggerFactory);
    }

    public async Task<IInbox> CreateInboxAsync(
        CreateInboxOptions? options = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("CreateInbox");
        activity?.SetTag("vaultsandbox.ttl", options?.Ttl?.TotalSeconds);

        try
        {
            // Generate ML-KEM-768 keypair
            var keyPair = _cryptoProvider.GenerateKeyPair();

            _logger?.LogDebug("Generated ML-KEM-768 keypair for new inbox");

            // Calculate TTL in seconds (use default from options if not specified)
            int ttlSeconds = options?.Ttl is not null
                ? (int)options.Ttl.Value.TotalSeconds
                : _options.DefaultInboxTtlSeconds;

            // Create inbox on server
            var request = new CreateInboxRequest
            {
                ClientKemPk = keyPair.PublicKeyB64,
                Ttl = ttlSeconds,
                EmailAddress = options?.EmailAddress
            };

            var response = await _apiClient.CreateInboxAsync(request, ct);

            _logger?.LogInformation(
                "Created inbox {EmailAddress} (expires: {ExpiresAt})",
                response.EmailAddress, response.ExpiresAt);

            activity?.SetTag("vaultsandbox.inbox.email", response.EmailAddress);
            activity?.SetTag("vaultsandbox.inbox.hash", response.InboxHash);
            VaultSandboxTelemetry.InboxesCreated.Add(1);

            // Create delivery strategy for this inbox
            var strategy = _deliveryStrategyFactory.Create(_options.DefaultDeliveryStrategy);
            _strategies[response.InboxHash] = strategy;

            return new Inbox(
                response.EmailAddress,
                response.ExpiresAt,
                response.InboxHash,
                response.ServerSigPk,
                keyPair,
                _apiClient,
                _cryptoProvider,
                strategy,
                _options,
                _loggerFactory?.CreateLogger<Inbox>());
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            VaultSandboxTelemetry.ApiErrors.Add(1);
            throw;
        }
    }

    public async Task DeleteInboxAsync(string emailAddress, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("DeleteInbox");
        activity?.SetTag("vaultsandbox.inbox.email", emailAddress);

        try
        {
            await _apiClient.DeleteInboxAsync(emailAddress, ct);
            _logger?.LogInformation("Deleted inbox {EmailAddress}", emailAddress);
            VaultSandboxTelemetry.InboxesDeleted.Add(1);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            VaultSandboxTelemetry.ApiErrors.Add(1);
            throw;
        }
    }

    public async Task<int> DeleteAllInboxesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("DeleteAllInboxes");

        try
        {
            var response = await _apiClient.DeleteAllInboxesAsync(ct);
            _logger?.LogInformation("Deleted {Count} inboxes", response.Deleted);
            activity?.SetTag("vaultsandbox.deleted.count", response.Deleted);
            VaultSandboxTelemetry.InboxesDeleted.Add(response.Deleted);
            return response.Deleted;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            VaultSandboxTelemetry.ApiErrors.Add(1);
            throw;
        }
    }

    public Task<IInbox> ImportInboxAsync(InboxExport export, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("ImportInbox");
        activity?.SetTag("vaultsandbox.inbox.email", export.EmailAddress);

        // Validate export data
        if (string.IsNullOrEmpty(export.EmailAddress))
            throw new InvalidImportDataException("EmailAddress is required");
        if (string.IsNullOrEmpty(export.PublicKeyB64))
            throw new InvalidImportDataException("PublicKeyB64 is required");
        if (string.IsNullOrEmpty(export.SecretKeyB64))
            throw new InvalidImportDataException("SecretKeyB64 is required");
        if (export.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidImportDataException("Inbox has expired");

        // Decode keys
        var publicKey = Base64Url.Decode(export.PublicKeyB64);
        var secretKey = Base64Url.Decode(export.SecretKeyB64);

        // Validate key sizes
        if (publicKey.Length != MlKemPublicKeySize)
            throw new InvalidImportDataException(
                $"Invalid public key size: {publicKey.Length} (expected {MlKemPublicKeySize})");
        if (secretKey.Length != MlKemSecretKeySize)
            throw new InvalidImportDataException(
                $"Invalid secret key size: {secretKey.Length} (expected {MlKemSecretKeySize})");

        var keyPair = new MlKemKeyPair
        {
            PublicKey = publicKey,
            SecretKey = secretKey
        };

        _logger?.LogInformation("Imported inbox {EmailAddress}", export.EmailAddress);

        // Create delivery strategy for this inbox
        var strategy = _deliveryStrategyFactory.Create(_options.DefaultDeliveryStrategy);
        _strategies[export.InboxHash] = strategy;

        var inbox = new Inbox(
            export.EmailAddress,
            export.ExpiresAt,
            export.InboxHash,
            export.ServerSigPk,
            keyPair,
            _apiClient,
            _cryptoProvider,
            strategy,
            _options,
            _loggerFactory?.CreateLogger<Inbox>());

        return Task.FromResult<IInbox>(inbox);
    }

    public async Task<bool> ValidateApiKeyAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("ValidateApiKey");

        try
        {
            var result = await _apiClient.CheckKeyAsync(ct);
            activity?.SetTag("vaultsandbox.api_key.valid", result.Ok);
            VaultSandboxTelemetry.ApiCalls.Add(1);
            return result.Ok;
        }
        catch (ApiException ex) when (ex.StatusCode == 401)
        {
            activity?.SetTag("vaultsandbox.api_key.valid", false);
            return false;
        }
    }

    public async Task<ServerInfo> GetServerInfoAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("GetServerInfo");

        try
        {
            var response = await _apiClient.GetServerInfoAsync(ct);
            VaultSandboxTelemetry.ApiCalls.Add(1);

            return new ServerInfo
            {
                ServerSigPk = response.ServerSigPk,
                Context = response.Context,
                MaxTtl = response.MaxTtl,
                DefaultTtl = response.DefaultTtl,
                SseConsole = response.SseConsole,
                AllowedDomains = response.AllowedDomains
            };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            VaultSandboxTelemetry.ApiErrors.Add(1);
            throw;
        }
    }

    public InboxMonitor MonitorInboxes(IReadOnlyList<IInbox> inboxes)
    {
        ThrowIfDisposed();

        if (inboxes.Count == 0)
            throw new ArgumentException("At least one inbox is required", nameof(inboxes));

        _logger?.LogDebug("Creating monitor for {Count} inboxes", inboxes.Count);

        return new InboxMonitor(inboxes);
    }

    public InboxMonitor MonitorInboxes(params IInbox[] inboxes)
    {
        return MonitorInboxes((IReadOnlyList<IInbox>)inboxes);
    }

    public async Task ExportInboxToFileAsync(
        IInbox inbox,
        string filePath,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        ArgumentNullException.ThrowIfNull(inbox);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var export = await inbox.ExportAsync();

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, json, ct);

        _logger?.LogInformation(
            "Exported inbox {EmailAddress} to {FilePath}",
            inbox.EmailAddress, filePath);
    }

    public async Task<IInbox> ImportInboxFromFileAsync(
        string filePath,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Inbox export file not found: {filePath}", filePath);
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(filePath, ct);
        }
        catch (IOException ex)
        {
            throw new InvalidImportDataException(
                $"Failed to read inbox export file: {ex.Message}");
        }

        InboxExport export;
        try
        {
            export = JsonSerializer.Deserialize<InboxExport>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new InvalidImportDataException(
                "Failed to parse inbox export file: null result");
        }
        catch (JsonException ex)
        {
            throw new InvalidImportDataException(
                $"Invalid JSON in inbox export file: {ex.Message}");
        }

        _logger?.LogInformation(
            "Importing inbox {EmailAddress} from {FilePath}",
            export.EmailAddress, filePath);

        return await ImportInboxAsync(export, ct);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(VaultSandboxClient));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        foreach (var strategy in _strategies.Values)
        {
            await strategy.DisposeAsync();
        }

        _strategies.Clear();

        if (_disposeApiClient)
        {
            _apiClient.Dispose();
        }

        _logger?.LogDebug("VaultSandboxClient disposed");
    }
}
