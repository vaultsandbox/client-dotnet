using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Http;

namespace VaultSandbox.Client.Extensions;

/// <summary>
/// Extension methods for registering VaultSandbox client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The default configuration section name for VaultSandbox options.
    /// </summary>
    public const string DefaultSectionName = "VaultSandbox";

    private const string HttpClientName = "VaultSandboxApiClient";

    /// <summary>
    /// Adds VaultSandbox client services using the root configuration.
    /// Binds options from the "VaultSandbox" configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The root configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // appsettings.json:
    /// // {
    /// //   "VaultSandbox": {
    /// //     "BaseUrl": "https://api.vaultsandbox.com",
    /// //     "ApiKey": "your-api-key"
    /// //   }
    /// // }
    ///
    /// builder.Services.AddVaultSandboxClient(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddVaultSandboxClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddVaultSandboxClient(configuration.GetSection(DefaultSectionName));
    }

    /// <summary>
    /// Adds VaultSandbox client services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for client options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddVaultSandboxClient(options =>
    /// {
    ///     options.BaseUrl = "https://api.vaultsandbox.com";
    ///     options.ApiKey = Environment.GetEnvironmentVariable("VAULTSANDBOX_API_KEY")!;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddVaultSandboxClient(
        this IServiceCollection services,
        Action<VaultSandboxClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<VaultSandboxClientOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddVaultSandboxClientCore();

        return services;
    }

    /// <summary>
    /// Adds VaultSandbox client services to the service collection with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddVaultSandboxClient(
    ///     builder.Configuration.GetSection("MyCustomSection"));
    /// </code>
    /// </example>
    public static IServiceCollection AddVaultSandboxClient(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.AddOptions<VaultSandboxClientOptions>()
            .Bind(configurationSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddVaultSandboxClientCore();

        return services;
    }

    /// <summary>
    /// Adds VaultSandbox client services using a builder action for advanced configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the client builder with access to the service provider.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddVaultSandboxClient((builder, sp) =>
    /// {
    ///     var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///     builder
    ///         .WithBaseUrl(config["VaultSandbox:BaseUrl"]!)
    ///         .WithApiKey(config["VaultSandbox:ApiKey"]!)
    ///         .WithLogging(sp.GetRequiredService&lt;ILoggerFactory&gt;());
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddVaultSandboxClient(
        this IServiceCollection services,
        Action<VaultSandboxClientBuilder, IServiceProvider> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton<ICryptoProvider, CryptoProvider>();

        services.AddScoped<IVaultSandboxClient>(sp =>
        {
            var builder = VaultSandboxClientBuilder.Create();
            configure(builder, sp);
            return builder.Build();
        });

        return services;
    }

    private static void AddVaultSandboxClientCore(this IServiceCollection services)
    {
        // Register options validator
        services.TryAddSingleton<IValidateOptions<VaultSandboxClientOptions>,
            VaultSandboxClientOptionsValidator>();

        // Register crypto provider as singleton (stateless)
        services.TryAddSingleton<ICryptoProvider, CryptoProvider>();

        // Register named HTTP client with resilience
        services.AddHttpClient(HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<VaultSandboxClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(options.HttpTimeoutMs);
            client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        })
        .AddStandardResilienceHandler();

        // Configure resilience options using the client options
        // The options are keyed by: "{HttpClientName}-standard"
        services.AddOptions<HttpStandardResilienceOptions>($"{HttpClientName}-standard")
            .Configure<IOptions<VaultSandboxClientOptions>>((resilienceOptions, clientOptionsAccessor) =>
            {
                var clientOptions = clientOptionsAccessor.Value;

                // Configure retry policy from client options
                resilienceOptions.Retry.MaxRetryAttempts = clientOptions.MaxRetries;
                resilienceOptions.Retry.Delay = TimeSpan.FromMilliseconds(clientOptions.RetryDelayMs);
                resilienceOptions.Retry.UseJitter = true;
                resilienceOptions.Retry.BackoffType = Polly.DelayBackoffType.Exponential;

                // Configure circuit breaker
                resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                resilienceOptions.CircuitBreaker.FailureRatio = 0.5;
                resilienceOptions.CircuitBreaker.MinimumThroughput = 10;
            });

        // Register API client using factory pattern (internal type)
        services.TryAddScoped<IVaultSandboxApiClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var logger = sp.GetService<ILogger<VaultSandboxApiClient>>();

            return new VaultSandboxApiClient(httpClient, logger);
        });

        // Register main client
        services.TryAddScoped<IVaultSandboxClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<VaultSandboxClientOptions>>().Value;
            var apiClient = sp.GetRequiredService<IVaultSandboxApiClient>();
            var cryptoProvider = sp.GetRequiredService<ICryptoProvider>();
            var loggerFactory = sp.GetService<ILoggerFactory>();

            return new VaultSandboxClient(apiClient, cryptoProvider, options, loggerFactory);
        });
    }
}
