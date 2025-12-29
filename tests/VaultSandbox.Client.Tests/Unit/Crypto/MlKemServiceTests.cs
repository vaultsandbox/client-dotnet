using FluentAssertions;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class MlKemServiceTests
{
    private readonly MlKemService _mlKemService = new();

    private const int PublicKeySize = 1184;
    private const int SecretKeySize = 2400;
    private const int SharedSecretSize = 32;

    [Fact]
    public void GenerateKeypair_ShouldReturnCorrectSizes()
    {
        // Act
        var (publicKey, secretKey) = _mlKemService.GenerateKeypair();

        // Assert
        publicKey.Should().HaveCount(PublicKeySize);
        secretKey.Should().HaveCount(SecretKeySize);
    }

    [Fact]
    public void GenerateKeypair_ShouldReturnDifferentKeysEachTime()
    {
        // Act
        var (publicKey1, secretKey1) = _mlKemService.GenerateKeypair();
        var (publicKey2, secretKey2) = _mlKemService.GenerateKeypair();

        // Assert
        publicKey1.Should().NotBeEquivalentTo(publicKey2);
        secretKey1.Should().NotBeEquivalentTo(secretKey2);
    }

    [Fact]
    public void Decapsulate_ValidInput_ShouldReturnSharedSecret()
    {
        // Arrange - Generate keypair and encapsulate
        var (publicKey, secretKey) = _mlKemService.GenerateKeypair();
        var (ciphertext, expectedSharedSecret) = Encapsulate(publicKey);

        // Act
        byte[] sharedSecret = _mlKemService.Decapsulate(ciphertext, secretKey);

        // Assert
        sharedSecret.Should().HaveCount(SharedSecretSize);
        sharedSecret.Should().BeEquivalentTo(expectedSharedSecret);
    }

    [Fact]
    public void Decapsulate_InvalidSecretKeySize_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] invalidSecretKey = new byte[100]; // Should be 2400
        byte[] ciphertext = new byte[1088]; // ML-KEM-768 ciphertext size

        // Act
        Action act = () => _mlKemService.Decapsulate(ciphertext, invalidSecretKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("secretKey");
    }

    [Fact]
    public void Decapsulate_InvalidCiphertext_ShouldThrowDecryptionException()
    {
        // Arrange
        var (_, secretKey) = _mlKemService.GenerateKeypair();
        byte[] invalidCiphertext = new byte[100]; // Invalid size

        // Act
        Action act = () => _mlKemService.Decapsulate(invalidCiphertext, secretKey);

        // Assert
        act.Should().Throw<DecryptionException>();
    }

    [Fact]
    public void ExtractPublicKey_ShouldReturnCorrectPublicKey()
    {
        // Arrange
        var (expectedPublicKey, secretKey) = _mlKemService.GenerateKeypair();

        // Act
        byte[] extractedPublicKey = _mlKemService.ExtractPublicKey(secretKey);

        // Assert
        extractedPublicKey.Should().HaveCount(PublicKeySize);
        extractedPublicKey.Should().BeEquivalentTo(expectedPublicKey);
    }

    [Fact]
    public void ExtractPublicKey_InvalidSecretKeySize_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] invalidSecretKey = new byte[100]; // Should be 2400

        // Act
        Action act = () => _mlKemService.ExtractPublicKey(invalidSecretKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("secretKey");
    }

    [Fact]
    public void RoundTrip_EncapsulateDecapsulate_ShouldProduceSameSharedSecret()
    {
        // Arrange
        var (publicKey, secretKey) = _mlKemService.GenerateKeypair();
        var (ciphertext, encapsulatedSecret) = Encapsulate(publicKey);

        // Act
        byte[] decapsulatedSecret = _mlKemService.Decapsulate(ciphertext, secretKey);

        // Assert
        decapsulatedSecret.Should().BeEquivalentTo(encapsulatedSecret);
    }

    /// <summary>
    /// Helper method to encapsulate using BouncyCastle directly for testing.
    /// </summary>
    private static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] publicKey)
    {
        var parameters = MLKemParameters.ml_kem_768;
        var publicKeyParams = MLKemPublicKeyParameters.FromEncoding(parameters, publicKey);

        var encapsulator = new MLKemEncapsulator(parameters);
        encapsulator.Init(publicKeyParams);

        byte[] ciphertext = new byte[encapsulator.EncapsulationLength];
        byte[] sharedSecret = new byte[encapsulator.SecretLength];
        encapsulator.Encapsulate(ciphertext, 0, ciphertext.Length, sharedSecret, 0, sharedSecret.Length);

        return (ciphertext, sharedSecret);
    }
}
