using FluentAssertions;
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
}
