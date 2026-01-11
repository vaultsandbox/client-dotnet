using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Http;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Factory for creating delivery strategies.
/// </summary>
internal sealed class DeliveryStrategyFactory
{
    private readonly IVaultSandboxApiClient _apiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILoggerFactory? _loggerFactory;

    public DeliveryStrategyFactory(
        IVaultSandboxApiClient apiClient,
        VaultSandboxClientOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        _apiClient = apiClient;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public IDeliveryStrategy Create(DeliveryStrategy strategy)
    {
        return strategy switch
        {
            DeliveryStrategy.Sse => new SseDeliveryStrategy(
                _apiClient,
                _options,
                _loggerFactory?.CreateLogger<SseDeliveryStrategy>()),

            DeliveryStrategy.Polling => new PollingDeliveryStrategy(
                _apiClient,
                _options,
                _loggerFactory?.CreateLogger<PollingDeliveryStrategy>()),

            _ => throw new ArgumentOutOfRangeException(nameof(strategy))
        };
    }
}
