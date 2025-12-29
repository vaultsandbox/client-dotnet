using System.Security.Cryptography;
using FluentAssertions;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class AesGcmServiceTests
{
    private readonly AesGcmService _aesGcmService = new();

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    [Fact]
    public void Decrypt_ValidCiphertext_ShouldReturnPlaintext()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = "associated-data"u8.ToArray();
        byte[] plaintext = "Hello, World!"u8.ToArray();
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        // Encrypt
        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Act
        byte[] decrypted = _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, aad);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void Decrypt_InvalidKeySize_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] invalidKey = new byte[16]; // Should be 32
        byte[] nonce = new byte[NonceSize];
        byte[] ciphertext = new byte[TagSize + 10];
        byte[] aad = [];

        // Act
        Action act = () => _aesGcmService.Decrypt(invalidKey, nonce, ciphertext, aad);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void Decrypt_InvalidNonceSize_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] invalidNonce = new byte[8]; // Should be 12
        byte[] ciphertext = new byte[TagSize + 10];
        byte[] aad = [];

        // Act
        Action act = () => _aesGcmService.Decrypt(key, invalidNonce, ciphertext, aad);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("nonce");
    }

    [Fact]
    public void Decrypt_CiphertextTooShort_ShouldThrowArgumentException()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] shortCiphertext = new byte[TagSize - 1]; // Less than tag size
        byte[] aad = [];

        // Act
        Action act = () => _aesGcmService.Decrypt(key, nonce, shortCiphertext, aad);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("ciphertext");
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ShouldThrowDecryptionException()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = "associated-data"u8.ToArray();
        byte[] plaintext = "Hello, World!"u8.ToArray();
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Tamper with ciphertext
        ciphertextWithTag[0] ^= 0xFF;

        // Act
        Action act = () => _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, aad);

        // Assert
        act.Should().Throw<DecryptionException>();
    }

    [Fact]
    public void Decrypt_TamperedTag_ShouldThrowDecryptionException()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = "associated-data"u8.ToArray();
        byte[] plaintext = "Hello, World!"u8.ToArray();
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Tamper with tag (last 16 bytes)
        ciphertextWithTag[^1] ^= 0xFF;

        // Act
        Action act = () => _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, aad);

        // Assert
        act.Should().Throw<DecryptionException>();
    }

    [Fact]
    public void Decrypt_TamperedAad_ShouldThrowDecryptionException()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = "associated-data"u8.ToArray();
        byte[] wrongAad = "wrong-data"u8.ToArray();
        byte[] plaintext = "Hello, World!"u8.ToArray();
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Act
        Action act = () => _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, wrongAad);

        // Assert
        act.Should().Throw<DecryptionException>();
    }

    [Fact]
    public void Decrypt_EmptyPlaintext_ShouldSucceed()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = [];
        byte[] plaintext = [];
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Act
        byte[] decrypted = _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, aad);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_EmptyAad_ShouldSucceed()
    {
        // Arrange
        byte[] key = new byte[KeySize];
        byte[] nonce = new byte[NonceSize];
        byte[] aad = [];
        byte[] plaintext = "Hello, World!"u8.ToArray();
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertextWithTag = Encrypt(key, nonce, plaintext, aad);

        // Act
        byte[] decrypted = _aesGcmService.Decrypt(key, nonce, ciphertextWithTag, aad);

        // Assert
        decrypted.Should().BeEquivalentTo(plaintext);
    }

    /// <summary>
    /// Helper method to encrypt data for testing decryption.
    /// </summary>
    private static byte[] Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] aad)
    {
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        // Append tag to ciphertext
        byte[] result = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(result, 0);
        tag.CopyTo(result, ciphertext.Length);

        return result;
    }
}
