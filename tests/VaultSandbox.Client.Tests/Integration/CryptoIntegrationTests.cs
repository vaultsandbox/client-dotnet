using FluentAssertions;
using VaultSandbox.Client.Crypto;
using Xunit;

namespace VaultSandbox.Client.Tests.Integration;

/// <summary>
/// Integration tests for cryptographic operations verifying interoperability.
/// </summary>
[Trait("Category", "Integration")]
public class CryptoIntegrationTests
{
    private readonly CryptoProvider _cryptoProvider = new();

    [Fact]
    public void MlKem_KeyGeneration_ShouldProduceValidSizedKeys()
    {
        // Act
        var keyPair = _cryptoProvider.GenerateKeyPair();

        // Assert
        keyPair.PublicKey.Should().HaveCount(1184);
        keyPair.SecretKey.Should().HaveCount(2400);
    }

    [Fact]
    public void MlKem_KeyGeneration_ShouldProduceUniqueKeys()
    {
        // Act
        var keyPair1 = _cryptoProvider.GenerateKeyPair();
        var keyPair2 = _cryptoProvider.GenerateKeyPair();

        // Assert
        keyPair1.PublicKey.Should().NotBeEquivalentTo(keyPair2.PublicKey);
        keyPair1.SecretKey.Should().NotBeEquivalentTo(keyPair2.SecretKey);
    }

    [Fact]
    public void MlKem_PublicKeyB64_ShouldBeValidBase64Url()
    {
        // Act
        var keyPair = _cryptoProvider.GenerateKeyPair();

        // Assert
        keyPair.PublicKeyB64.Should().NotBeNullOrEmpty();

        // Verify it can be decoded back
        var decoded = Base64Url.Decode(keyPair.PublicKeyB64);
        decoded.Should().BeEquivalentTo(keyPair.PublicKey);
    }

    [Fact]
    public void MlKem_SecretKeyB64_ShouldBeValidBase64Url()
    {
        // Act
        var keyPair = _cryptoProvider.GenerateKeyPair();

        // Assert
        keyPair.SecretKeyB64.Should().NotBeNullOrEmpty();

        // Verify it can be decoded back
        var decoded = Base64Url.Decode(keyPair.SecretKeyB64);
        decoded.Should().BeEquivalentTo(keyPair.SecretKey);
    }

    [Fact]
    public void Base64Url_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new byte[256];
        Random.Shared.NextBytes(original);

        // Act
        var encoded = Base64Url.Encode(original);
        var decoded = Base64Url.Decode(encoded);

        // Assert
        decoded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Base64Url_EmptyInput_ShouldWork()
    {
        // Arrange
        var original = Array.Empty<byte>();

        // Act
        var encoded = Base64Url.Encode(original);
        var decoded = Base64Url.Decode(encoded);

        // Assert
        encoded.Should().BeEmpty();
        decoded.Should().BeEmpty();
    }

    [Fact]
    public void Base64Url_ShouldNotContainStandardBase64Characters()
    {
        // Arrange - Use data that would produce + and / in standard base64
        var data = new byte[] { 0xfb, 0xff, 0xfe, 0xfb, 0xff, 0xfe };

        // Act
        var encoded = Base64Url.Encode(data);

        // Assert - Should use URL-safe characters
        encoded.Should().NotContain("+");
        encoded.Should().NotContain("/");
        encoded.Should().NotContain("=");
    }

    [Fact]
    public async Task GenerateKeyPair_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<MlKemKeyPair>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _cryptoProvider.GenerateKeyPair()));
        }

        var keyPairs = await Task.WhenAll(tasks);

        // Assert - All key pairs should be unique
        var publicKeys = keyPairs.Select(kp => Convert.ToBase64String(kp.PublicKey)).ToList();
        publicKeys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateKeyPair_MultipleCalls_ShouldNotThrow()
    {
        // Act
        Action act = () =>
        {
            for (int i = 0; i < 100; i++)
            {
                var keyPair = _cryptoProvider.GenerateKeyPair();
                keyPair.Should().NotBeNull();
            }
        };

        // Assert
        act.Should().NotThrow();
    }
}
