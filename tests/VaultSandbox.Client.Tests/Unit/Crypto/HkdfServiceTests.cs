using FluentAssertions;
using VaultSandbox.Client.Crypto;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class HkdfServiceTests
{
    private readonly HkdfService _hkdfService = new();

    [Fact]
    public void DeriveKey_ShouldReturn32Bytes()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem = new byte[1088]; // ML-KEM-768 ciphertext size
        byte[] aad = "test-aad"u8.ToArray();
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem);

        // Act
        byte[] key = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // Assert
        key.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveKey_WithEmptyAad_ShouldSucceed()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem = new byte[1088];
        byte[] aad = [];
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem);

        // Act
        byte[] key = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // Assert
        key.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveKey_WithLargeAad_ShouldSucceed()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem = new byte[1088];
        byte[] aad = new byte[10000];
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem);
        Random.Shared.NextBytes(aad);

        // Act
        byte[] key = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // Assert
        key.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveKey_SameInputs_ShouldProduceSameOutput()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem = new byte[1088];
        byte[] aad = "test-aad"u8.ToArray();
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem);

        // Act
        byte[] key1 = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);
        byte[] key2 = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // Assert
        key1.Should().BeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKey_DifferentSharedSecrets_ShouldProduceDifferentOutputs()
    {
        // Arrange
        byte[] sharedSecret1 = new byte[32];
        byte[] sharedSecret2 = new byte[32];
        byte[] ctKem = new byte[1088];
        byte[] aad = "test-aad"u8.ToArray();
        Random.Shared.NextBytes(sharedSecret1);
        Random.Shared.NextBytes(sharedSecret2);
        Random.Shared.NextBytes(ctKem);

        // Act
        byte[] key1 = _hkdfService.DeriveKey(sharedSecret1, ctKem, aad);
        byte[] key2 = _hkdfService.DeriveKey(sharedSecret2, ctKem, aad);

        // Assert
        key1.Should().NotBeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKey_DifferentCtKem_ShouldProduceDifferentOutputs()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem1 = new byte[1088];
        byte[] ctKem2 = new byte[1088];
        byte[] aad = "test-aad"u8.ToArray();
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem1);
        Random.Shared.NextBytes(ctKem2);

        // Act
        byte[] key1 = _hkdfService.DeriveKey(sharedSecret, ctKem1, aad);
        byte[] key2 = _hkdfService.DeriveKey(sharedSecret, ctKem2, aad);

        // Assert
        key1.Should().NotBeEquivalentTo(key2);
    }

    [Fact]
    public void DeriveKey_DifferentAad_ShouldProduceDifferentOutputs()
    {
        // Arrange
        byte[] sharedSecret = new byte[32];
        byte[] ctKem = new byte[1088];
        byte[] aad1 = "aad-1"u8.ToArray();
        byte[] aad2 = "aad-2"u8.ToArray();
        Random.Shared.NextBytes(sharedSecret);
        Random.Shared.NextBytes(ctKem);

        // Act
        byte[] key1 = _hkdfService.DeriveKey(sharedSecret, ctKem, aad1);
        byte[] key2 = _hkdfService.DeriveKey(sharedSecret, ctKem, aad2);

        // Assert
        key1.Should().NotBeEquivalentTo(key2);
    }
}
