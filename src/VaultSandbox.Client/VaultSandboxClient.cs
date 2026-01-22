using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Delivery;
using VaultSandbox.Client.Diagnostics;
using static VaultSandbox.Client.Diagnostics.Log;
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
            // Calculate TTL in seconds (use default from options if not specified)
            int ttlSeconds = options?.Ttl is not null
                ? (int)options.Ttl.Value.TotalSeconds
                : _options.DefaultInboxTtlSeconds;

            // Determine encryption mode
            string? encryptionParam = options?.Encryption switch
            {
                InboxEncryption.Encrypted => "encrypted",
                InboxEncryption.Plain => "plain",
                _ => null
            };

            // Generate keypair only if we need encryption
            // If explicitly requesting plain, don't generate keys
            // If no encryption specified, generate keys (server will decide based on policy)
            MlKemKeyPair? keyPair = null;
            if (options?.Encryption != InboxEncryption.Plain)
            {
                keyPair = _cryptoProvider.GenerateKeyPair();
                Log.Debug(_logger, "Generated ML-KEM-768 keypair for new inbox");
            }

            // Create inbox on server
            var request = new CreateInboxRequest
            {
                ClientKemPk = keyPair?.PublicKeyB64,
                Ttl = ttlSeconds,
                EmailAddress = options?.EmailAddress,
                EmailAuth = options?.EmailAuth,
                Encryption = encryptionParam,
                SpamAnalysis = options?.SpamAnalysis,
                Chaos = options?.Chaos?.ToRequest()
            };

            var response = await _apiClient.CreateInboxAsync(request, ct);

            Information(_logger, "Created inbox {EmailAddress} (expires: {ExpiresAt})",
                response.EmailAddress, response.ExpiresAt);
            Debug(_logger, "Inbox encryption: {Encrypted}", response.Encrypted);

            activity?.SetTag("vaultsandbox.inbox.email", response.EmailAddress);
            activity?.SetTag("vaultsandbox.inbox.hash", response.InboxHash);
            activity?.SetTag("vaultsandbox.inbox.encrypted", response.Encrypted);
            VaultSandboxTelemetry.InboxesCreated.Add(1);

            // Create delivery strategy for this inbox
            var strategy = _deliveryStrategyFactory.Create(_options.DefaultDeliveryStrategy);
            _strategies[response.InboxHash] = strategy;

            if (response.Encrypted)
            {
                // Encrypted inbox - requires keypair and server signing key
                if (keyPair is null)
                {
                    throw new InvalidOperationException(
                        "Server created an encrypted inbox but no keypair was generated. " +
                        "This may indicate a server policy mismatch.");
                }

                if (response.ServerSigPk is null)
                {
                    throw new InvalidOperationException(
                        "Server created an encrypted inbox but did not return serverSigPk.");
                }

                return new Inbox(
                    response.EmailAddress,
                    response.ExpiresAt,
                    response.InboxHash,
                    response.EmailAuth,
                    response.ServerSigPk,
                    keyPair,
                    _apiClient,
                    _cryptoProvider,
                    strategy,
                    _options,
                    _loggerFactory?.CreateLogger<Inbox>());
            }
            else
            {
                // Plain inbox - no encryption needed
                return new Inbox(
                    response.EmailAddress,
                    response.ExpiresAt,
                    response.InboxHash,
                    response.EmailAuth,
                    _apiClient,
                    strategy,
                    _options,
                    _loggerFactory?.CreateLogger<Inbox>());
            }
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
            Information(_logger, "Deleted inbox {EmailAddress}", emailAddress);
            VaultSandboxTelemetry.InboxesDeleted.Add(1);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            VaultSandboxTelemetry.ApiErrors.Add(1);
            throw;
        }
    }

    /// <summary>
    /// Deletes all inboxes associated with the current API key.
    /// WARNING: This method should NEVER be called in tests as it will delete all server inboxes
    /// and interfere with other tests running in parallel.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public async Task<int> DeleteAllInboxesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("DeleteAllInboxes");

        try
        {
            var response = await _apiClient.DeleteAllInboxesAsync(ct);
            Information(_logger, "Deleted {Count} inboxes", response.Deleted);
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

    /// <summary>
    /// Expected ML-DSA-65 public key size in bytes.
    /// </summary>
    internal const int MlDsaPublicKeySize = 1952;

    /// <summary>
    /// Offset of public key within ML-KEM-768 secret key.
    /// </summary>
    private const int MlKemPublicKeyOffset = 1152;

    public Task<IInbox> ImportInboxAsync(InboxExport export, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        using var activity = VaultSandboxTelemetry.StartActivity("ImportInbox");
        activity?.SetTag("vaultsandbox.inbox.email", export.EmailAddress);

        // Per spec Section 10.1: Validate in order

        // Step 2: Validate version
        if (export.Version != 1)
            throw new InvalidImportDataException(
                $"Unsupported export version: {export.Version} (expected 1)");

        // Step 3: Validate required fields
        if (string.IsNullOrEmpty(export.EmailAddress))
            throw new InvalidImportDataException("EmailAddress is required");
        if (string.IsNullOrEmpty(export.InboxHash))
            throw new InvalidImportDataException("InboxHash is required");

        // Step 4: Validate emailAddress format (must contain exactly one @)
        int atCount = export.EmailAddress.Count(c => c == '@');
        if (atCount != 1)
            throw new InvalidImportDataException(
                $"Invalid email address format: must contain exactly one '@' character (found {atCount})");

        // Check expiration
        if (export.ExpiresAt < DateTimeOffset.UtcNow)
            throw new InvalidImportDataException("Inbox has expired");

        // Create delivery strategy for this inbox
        var strategy = _deliveryStrategyFactory.Create(_options.DefaultDeliveryStrategy);
        _strategies[export.InboxHash] = strategy;

        // Handle plain (non-encrypted) inbox
        if (!export.Encrypted)
        {
            Information(_logger, "Imported plain inbox {EmailAddress}", export.EmailAddress);

            var plainInbox = new Inbox(
                export.EmailAddress,
                export.ExpiresAt,
                export.InboxHash,
                export.EmailAuth,
                _apiClient,
                strategy,
                _options,
                _loggerFactory?.CreateLogger<Inbox>());

            return Task.FromResult<IInbox>(plainInbox);
        }

        // Handle encrypted inbox - validate encryption-specific fields
        if (string.IsNullOrEmpty(export.SecretKey))
            throw new InvalidImportDataException("SecretKey is required for encrypted inbox");
        if (string.IsNullOrEmpty(export.ServerSigPk))
            throw new InvalidImportDataException("ServerSigPk is required for encrypted inbox");

        // Step 6: Validate and decode secretKey
        byte[] secretKey;
        try
        {
            secretKey = Base64Url.Decode(export.SecretKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidImportDataException($"Invalid SecretKey encoding: {ex.Message}");
        }

        if (secretKey.Length != MlKemSecretKeySize)
            throw new InvalidImportDataException(
                $"Invalid secret key size: {secretKey.Length} (expected {MlKemSecretKeySize})");

        // Step 7: Validate and decode serverSigPk
        byte[] serverSigPkBytes;
        try
        {
            serverSigPkBytes = Base64Url.Decode(export.ServerSigPk);
        }
        catch (FormatException ex)
        {
            throw new InvalidImportDataException($"Invalid ServerSigPk encoding: {ex.Message}");
        }

        if (serverSigPkBytes.Length != MlDsaPublicKeySize)
            throw new InvalidImportDataException(
                $"Invalid server signature public key size: {serverSigPkBytes.Length} (expected {MlDsaPublicKeySize})");

        // Per spec Section 10.2: Derive public key from secret key
        // Public key is at bytes [1152:2400] of secret key
        var publicKey = secretKey.AsSpan(MlKemPublicKeyOffset, MlKemPublicKeySize).ToArray();

        var keyPair = new MlKemKeyPair
        {
            PublicKey = publicKey,
            SecretKey = secretKey
        };

        Information(_logger, "Imported encrypted inbox {EmailAddress}", export.EmailAddress);

        var inbox = new Inbox(
            export.EmailAddress,
            export.ExpiresAt,
            export.InboxHash,
            export.EmailAuth,
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
                AllowedDomains = response.AllowedDomains,
                EncryptionPolicy = response.EncryptionPolicy,
                SpamAnalysisEnabled = response.SpamAnalysisEnabled,
                ChaosEnabled = response.ChaosEnabled
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

        Debug(_logger, "Creating monitor for {Count} inboxes", inboxes.Count);

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

        Information(_logger, "Exported inbox {EmailAddress} to {FilePath}",
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

        Information(_logger, "Importing inbox {EmailAddress} from {FilePath}",
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

        Debug(_logger, "VaultSandboxClient disposed");
    }
}
