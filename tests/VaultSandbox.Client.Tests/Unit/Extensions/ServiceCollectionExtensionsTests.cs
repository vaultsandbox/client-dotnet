using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Extensions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddVaultSandboxClient_WithConfigure_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();

        // Options should be registered
        var optionsInstance = provider.GetService<IOptions<VaultSandboxClientOptions>>();
        optionsInstance.Should().NotBeNull();
        optionsInstance!.Value.BaseUrl.Should().Be("https://test.example.com");
        optionsInstance.Value.ApiKey.Should().Be("test-api-key");

        // Crypto provider should be registered as singleton
        var cryptoProvider1 = provider.GetService<ICryptoProvider>();
        var cryptoProvider2 = provider.GetService<ICryptoProvider>();
        cryptoProvider1.Should().NotBeNull();
        cryptoProvider1.Should().BeSameAs(cryptoProvider2);
    }

    [Fact]
    public void AddVaultSandboxClient_ShouldRegisterOptionsValidator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<VaultSandboxClientOptions>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void AddVaultSandboxClient_ShouldRegisterHttpClientFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    [Fact]
    public async Task AddVaultSandboxClient_ShouldRegisterVaultSandboxClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var client = scope.ServiceProvider.GetService<IVaultSandboxClient>();
            client.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AddVaultSandboxClient_VaultSandboxClient_ShouldBeScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();

        var scope1 = provider.CreateAsyncScope();
        var scope2 = provider.CreateAsyncScope();

        var client1 = scope1.ServiceProvider.GetService<IVaultSandboxClient>();
        var client2 = scope2.ServiceProvider.GetService<IVaultSandboxClient>();

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().NotBeSameAs(client2);

        await scope1.DisposeAsync();
        await scope2.DisposeAsync();
    }

    [Fact]
    public void AddVaultSandboxClient_CryptoProvider_ShouldBeSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var crypto1 = scope1.ServiceProvider.GetService<ICryptoProvider>();
        var crypto2 = scope2.ServiceProvider.GetService<ICryptoProvider>();

        crypto1.Should().NotBeNull();
        crypto2.Should().NotBeNull();
        crypto1.Should().BeSameAs(crypto2);
    }

    [Fact]
    public void AddVaultSandboxClient_NullServices_ShouldThrow()
    {
        // Act
        Action act = () => ServiceCollectionExtensions.AddVaultSandboxClient(null!, _ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddVaultSandboxClient_NullConfigure_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultSandboxClient((Action<VaultSandboxClientOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configure");
    }

    [Fact]
    public void AddVaultSandboxClient_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test.example.com";
            options.ApiKey = "test-api-key";
        });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddVaultSandboxClient_CalledMultipleTimes_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test1.example.com";
            options.ApiKey = "test-api-key-1";
        });

        services.AddVaultSandboxClient(options =>
        {
            options.BaseUrl = "https://test2.example.com";
            options.ApiKey = "test-api-key-2";
        });

        // Assert
        var provider = services.BuildServiceProvider();

        // CryptoProvider uses TryAddSingleton, so only one should be registered
        var cryptoProviders = services.Where(d => d.ServiceType == typeof(ICryptoProvider));
        cryptoProviders.Should().HaveCount(1);
    }

    #region IConfiguration Overload Tests

    [Fact]
    public void AddVaultSandboxClient_WithIConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VaultSandbox:BaseUrl"] = "https://config.example.com",
                ["VaultSandbox:ApiKey"] = "config-api-key"
            })
            .Build();

        // Act
        services.AddVaultSandboxClient(configuration);

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsInstance = provider.GetService<IOptions<VaultSandboxClientOptions>>();
        optionsInstance.Should().NotBeNull();
        optionsInstance!.Value.BaseUrl.Should().Be("https://config.example.com");
        optionsInstance.Value.ApiKey.Should().Be("config-api-key");
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfiguration_NullServices_ShouldThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();

        // Act
        Action act = () => ServiceCollectionExtensions.AddVaultSandboxClient(null!, configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfiguration_NullConfiguration_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultSandboxClient((IConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configuration");
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfiguration_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VaultSandbox:BaseUrl"] = "https://test.example.com",
                ["VaultSandbox:ApiKey"] = "test-api-key"
            })
            .Build();

        // Act
        var result = services.AddVaultSandboxClient(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void DefaultSectionName_ShouldBeVaultSandbox()
    {
        // Assert
        ServiceCollectionExtensions.DefaultSectionName.Should().Be("VaultSandbox");
    }

    #endregion

    #region IConfigurationSection Overload Tests

    [Fact]
    public void AddVaultSandboxClient_WithIConfigurationSection_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomSection:BaseUrl"] = "https://section.example.com",
                ["CustomSection:ApiKey"] = "section-api-key",
                ["CustomSection:MaxRetries"] = "5"
            })
            .Build();

        // Act
        services.AddVaultSandboxClient(configuration.GetSection("CustomSection"));

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsInstance = provider.GetService<IOptions<VaultSandboxClientOptions>>();
        optionsInstance.Should().NotBeNull();
        optionsInstance!.Value.BaseUrl.Should().Be("https://section.example.com");
        optionsInstance.Value.ApiKey.Should().Be("section-api-key");
        optionsInstance.Value.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfigurationSection_NullServices_ShouldThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var section = configuration.GetSection("Test");

        // Act
        Action act = () => ServiceCollectionExtensions.AddVaultSandboxClient(null!, section);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfigurationSection_NullSection_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultSandboxClient((IConfigurationSection)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configurationSection");
    }

    [Fact]
    public void AddVaultSandboxClient_WithIConfigurationSection_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomSection:BaseUrl"] = "https://test.example.com",
                ["CustomSection:ApiKey"] = "test-api-key"
            })
            .Build();

        // Act
        var result = services.AddVaultSandboxClient(configuration.GetSection("CustomSection"));

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region Builder Action Overload Tests

    [Fact]
    public async Task AddVaultSandboxClient_WithBuilderAction_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient((builder, _) =>
        {
            builder
                .WithBaseUrl("https://builder.example.com")
                .WithApiKey("builder-api-key");
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var client = scope.ServiceProvider.GetService<IVaultSandboxClient>();
            client.Should().NotBeNull();
        }
    }

    [Fact]
    public void AddVaultSandboxClient_WithBuilderAction_ShouldRegisterCryptoProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddVaultSandboxClient((builder, _) =>
        {
            builder
                .WithBaseUrl("https://builder.example.com")
                .WithApiKey("builder-api-key");
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var cryptoProvider = provider.GetService<ICryptoProvider>();
        cryptoProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task AddVaultSandboxClient_WithBuilderAction_ShouldProvideServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestValue"] = "FromConfig"
            })
            .Build());

        IServiceProvider? capturedServiceProvider = null;

        // Act
        services.AddVaultSandboxClient((builder, sp) =>
        {
            capturedServiceProvider = sp;
            var config = sp.GetRequiredService<IConfiguration>();
            builder
                .WithBaseUrl("https://builder.example.com")
                .WithApiKey(config["TestValue"]!);
        });

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var client = scope.ServiceProvider.GetService<IVaultSandboxClient>();

            // Assert
            client.Should().NotBeNull();
            capturedServiceProvider.Should().NotBeNull();
        }
    }

    [Fact]
    public void AddVaultSandboxClient_WithBuilderAction_NullServices_ShouldThrow()
    {
        // Act
        Action act = () => ServiceCollectionExtensions.AddVaultSandboxClient(
            null!,
            (Action<VaultSandboxClientBuilder, IServiceProvider>)((_, _) => { }));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("services");
    }

    [Fact]
    public void AddVaultSandboxClient_WithBuilderAction_NullConfigure_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddVaultSandboxClient(
            (Action<VaultSandboxClientBuilder, IServiceProvider>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("configure");
    }

    [Fact]
    public void AddVaultSandboxClient_WithBuilderAction_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddVaultSandboxClient((builder, _) =>
        {
            builder
                .WithBaseUrl("https://builder.example.com")
                .WithApiKey("builder-api-key");
        });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task AddVaultSandboxClient_WithBuilderAction_ClientShouldBeScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddVaultSandboxClient((builder, _) =>
        {
            builder
                .WithBaseUrl("https://builder.example.com")
                .WithApiKey("builder-api-key");
        });

        // Assert
        var provider = services.BuildServiceProvider();

        var scope1 = provider.CreateAsyncScope();
        var scope2 = provider.CreateAsyncScope();

        var client1 = scope1.ServiceProvider.GetService<IVaultSandboxClient>();
        var client2 = scope2.ServiceProvider.GetService<IVaultSandboxClient>();

        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().NotBeSameAs(client2);

        await scope1.DisposeAsync();
        await scope2.DisposeAsync();
    }

    #endregion
}
