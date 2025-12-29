using FluentAssertions;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class MlDsaServiceTests
{
    private readonly MlDsaService _mlDsaService = new();

    private const int PublicKeySize = 1952;

    [Fact]
    public void Verify_ValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey);

        // Act
        bool result = _mlDsaService.Verify(signature, message, publicKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_InvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey);

        // Corrupt the signature
        signature[0] ^= 0xFF;

        // Act
        bool result = _mlDsaService.Verify(signature, message, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongMessage_ShouldReturnFalse()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] originalMessage = "Original message"u8.ToArray();
        byte[] differentMessage = "Different message"u8.ToArray();
        byte[] signature = Sign(originalMessage, privateKey);

        // Act
        bool result = _mlDsaService.Verify(signature, differentMessage, publicKey);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WrongPublicKey_ShouldReturnFalse()
    {
        // Arrange
        var (_, privateKey1) = GenerateKeyPair();
        var (publicKey2, _) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey1);

        // Act - verify with different public key
        bool result = _mlDsaService.Verify(signature, message, publicKey2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidPublicKeySize_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] invalidPublicKey = new byte[100]; // Should be 1952
        byte[] message = "Test message"u8.ToArray();
        byte[] signature = new byte[3309]; // ML-DSA-65 signature size

        // Act
        Action act = () => _mlDsaService.Verify(signature, message, invalidPublicKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("publicKey");
    }

    [Fact]
    public void VerifyOrThrow_ValidSignature_ShouldNotThrow()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey);

        // Act
        Action act = () => _mlDsaService.VerifyOrThrow(signature, message, publicKey);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void VerifyOrThrow_InvalidSignature_ShouldThrowSignatureVerificationException()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey);

        // Corrupt the signature
        signature[0] ^= 0xFF;

        // Act
        Action act = () => _mlDsaService.VerifyOrThrow(signature, message, publicKey);

        // Assert
        act.Should().Throw<SignatureVerificationException>();
    }

    [Fact]
    public void VerifyOrThrow_TamperedMessage_ShouldThrowSignatureVerificationException()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = "Test message to sign"u8.ToArray();
        byte[] signature = Sign(message, privateKey);

        // Tamper with message
        message[0] ^= 0xFF;

        // Act
        Action act = () => _mlDsaService.VerifyOrThrow(signature, message, publicKey);

        // Assert
        act.Should().Throw<SignatureVerificationException>();
    }

    [Fact]
    public void Verify_EmptyMessage_ShouldWork()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = [];
        byte[] signature = Sign(message, privateKey);

        // Act
        bool result = _mlDsaService.Verify(signature, message, publicKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_LargeMessage_ShouldWork()
    {
        // Arrange
        var (publicKey, privateKey) = GenerateKeyPair();
        byte[] message = new byte[100000];
        Random.Shared.NextBytes(message);
        byte[] signature = Sign(message, privateKey);

        // Act
        bool result = _mlDsaService.Verify(signature, message, publicKey);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Helper method to generate ML-DSA-65 keypair using BouncyCastle directly.
    /// </summary>
    private static (byte[] PublicKey, MLDsaPrivateKeyParameters PrivateKey) GenerateKeyPair()
    {
        var parameters = MLDsaParameters.ml_dsa_65;
        var keyGenParams = new MLDsaKeyGenerationParameters(new SecureRandom(), parameters);

        var keyPairGenerator = new MLDsaKeyPairGenerator();
        keyPairGenerator.Init(keyGenParams);

        var keyPair = keyPairGenerator.GenerateKeyPair();

        var publicKey = (MLDsaPublicKeyParameters)keyPair.Public;
        var privateKey = (MLDsaPrivateKeyParameters)keyPair.Private;

        return (publicKey.GetEncoded(), privateKey);
    }

    /// <summary>
    /// Helper method to sign a message using BouncyCastle directly.
    /// </summary>
    private static byte[] Sign(byte[] message, MLDsaPrivateKeyParameters privateKey)
    {
        var signer = new MLDsaSigner(MLDsaParameters.ml_dsa_65, true);
        signer.Init(forSigning: true, privateKey);
        signer.BlockUpdate(message, 0, message.Length);
        return signer.GenerateSignature();
    }
}
