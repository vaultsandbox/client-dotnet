using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Exceptions;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Crypto;

public class CryptoProviderTests
{
    private readonly CryptoProvider _cryptoProvider = new();

    [Fact]
    public void GenerateKeyPair_ShouldReturnValidKeyPair()
    {
        // Act
        MlKemKeyPair keyPair = _cryptoProvider.GenerateKeyPair();

        // Assert
        keyPair.PublicKey.Should().HaveCount(1184);
        keyPair.SecretKey.Should().HaveCount(2400);
        keyPair.PublicKeyB64.Should().NotBeEmpty();
        keyPair.SecretKeyB64.Should().NotBeEmpty();
    }

    [Fact]
    public void Decrypt_ValidPayload_ShouldReturnPlaintext()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string expectedPlaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(expectedPlaintext)
        );

        // Act - use the server signing key from the payload as the expected key
        byte[] decrypted = _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, payload.ServerSigPk);

        // Assert
        Encoding.UTF8.GetString(decrypted).Should().Be(expectedPlaintext);
    }

    [Fact]
    public async Task DecryptAsync_ValidPayload_ShouldReturnPlaintext()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string expectedPlaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(expectedPlaintext)
        );

        // Act - use the server signing key from the payload as the expected key
        byte[] decrypted = await _cryptoProvider.DecryptAsync(payload, clientKeyPair.SecretKey, payload.ServerSigPk);

        // Assert
        Encoding.UTF8.GetString(decrypted).Should().Be(expectedPlaintext);
    }

    [Fact]
    public void Decrypt_InvalidSignature_ShouldThrowSignatureVerificationException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string plaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );
        string expectedServerSigPk = payload.ServerSigPk;

        // Corrupt the signature
        byte[] corruptedSig = Base64Url.Decode(payload.Signature);
        corruptedSig[0] ^= 0xFF;
        payload = payload with { Signature = Base64Url.Encode(corruptedSig) };

        // Act
        Action act = () => _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, expectedServerSigPk);

        // Assert - Signature verification should fail FIRST (before decryption)
        act.Should().Throw<SignatureVerificationException>();
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ShouldThrowSignatureVerificationException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string plaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );
        string expectedServerSigPk = payload.ServerSigPk;

        // Tamper with ciphertext (signature check should catch this first)
        byte[] tamperedCiphertext = Base64Url.Decode(payload.Ciphertext);
        tamperedCiphertext[0] ^= 0xFF;
        payload = payload with { Ciphertext = Base64Url.Encode(tamperedCiphertext) };

        // Act
        Action act = () => _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, expectedServerSigPk);

        // Assert - Should fail at signature verification (which checks ciphertext)
        act.Should().Throw<SignatureVerificationException>();
    }

    [Fact]
    public void Decrypt_WrongSecretKey_ShouldThrowDecryptionException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair1 = _cryptoProvider.GenerateKeyPair();
        MlKemKeyPair clientKeyPair2 = _cryptoProvider.GenerateKeyPair();
        string plaintext = "Hello, VaultSandbox!";

        // Encrypt for clientKeyPair1
        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair1.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );

        // Act - Try to decrypt with clientKeyPair2's secret key
        Action act = () => _cryptoProvider.Decrypt(payload, clientKeyPair2.SecretKey, payload.ServerSigPk);

        // Assert - Should fail at AES-GCM decryption (signature is valid)
        act.Should().Throw<DecryptionException>();
    }

    [Fact]
    public async Task DecryptAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            "Test"u8.ToArray()
        );
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = () => _cryptoProvider.DecryptAsync(payload, clientKeyPair.SecretKey, payload.ServerSigPk, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Decrypt_EmptyPlaintext_ShouldSucceed()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            [] // Empty plaintext
        );

        // Act
        byte[] decrypted = _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, payload.ServerSigPk);

        // Assert
        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_LargePlaintext_ShouldSucceed()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        byte[] largePlaintext = new byte[100000];
        Random.Shared.NextBytes(largePlaintext);

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            largePlaintext
        );

        // Act
        byte[] decrypted = _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, payload.ServerSigPk);

        // Assert
        decrypted.Should().BeEquivalentTo(largePlaintext);
    }

    [Fact]
    public void Decrypt_MismatchedServerSigningKey_ShouldThrowServerKeyMismatchException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string plaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );

        // Create a different expected key (simulating key substitution attack)
        EncryptedPayload differentKeyPayload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );
        string differentExpectedKey = differentKeyPayload.ServerSigPk;

        // Act - Try to decrypt with mismatched expected server signing key
        Action act = () => _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, differentExpectedKey);

        // Assert - Should fail with ServerKeyMismatchException BEFORE any other validation
        act.Should().Throw<ServerKeyMismatchException>()
            .Which.ExpectedKey.Should().Be(differentExpectedKey);
    }

    [Fact]
    public void Decrypt_TamperedServerSigningKeyInPayload_ShouldThrowServerKeyMismatchException()
    {
        // Arrange
        MlKemKeyPair clientKeyPair = _cryptoProvider.GenerateKeyPair();
        string plaintext = "Hello, VaultSandbox!";

        EncryptedPayload payload = CreateValidEncryptedPayload(
            clientKeyPair.PublicKey,
            Encoding.UTF8.GetBytes(plaintext)
        );
        string originalServerSigPk = payload.ServerSigPk;

        // Tamper with the server signing key in the payload (simulating attack)
        byte[] tamperedKey = Base64Url.Decode(payload.ServerSigPk);
        tamperedKey[0] ^= 0xFF;
        payload = payload with { ServerSigPk = Base64Url.Encode(tamperedKey) };

        // Act - Try to decrypt with the original (expected) server signing key
        Action act = () => _cryptoProvider.Decrypt(payload, clientKeyPair.SecretKey, originalServerSigPk);

        // Assert - Should fail with ServerKeyMismatchException
        act.Should().Throw<ServerKeyMismatchException>();
    }

    /// <summary>
    /// Creates a valid encrypted payload for testing.
    /// Simulates what the server would produce.
    /// </summary>
    private static EncryptedPayload CreateValidEncryptedPayload(byte[] clientPublicKey, byte[] plaintext)
    {
        const string context = "vaultsandbox:email:v1";

        // Generate server's signing keypair
        var sigParams = MLDsaParameters.ml_dsa_65;
        var sigKeyGenParams = new MLDsaKeyGenerationParameters(new SecureRandom(), sigParams);
        var sigKeyPairGenerator = new MLDsaKeyPairGenerator();
        sigKeyPairGenerator.Init(sigKeyGenParams);
        var sigKeyPair = sigKeyPairGenerator.GenerateKeyPair();
        var serverSigPk = (MLDsaPublicKeyParameters)sigKeyPair.Public;
        var serverSigSk = (MLDsaPrivateKeyParameters)sigKeyPair.Private;

        // KEM encapsulation
        var kemParams = MLKemParameters.ml_kem_768;
        var clientKemPk = MLKemPublicKeyParameters.FromEncoding(kemParams, clientPublicKey);

        var encapsulator = new MLKemEncapsulator(kemParams);
        encapsulator.Init(clientKemPk);

        byte[] ctKem = new byte[encapsulator.EncapsulationLength];
        byte[] sharedSecret = new byte[encapsulator.SecretLength];
        encapsulator.Encapsulate(ctKem, 0, ctKem.Length, sharedSecret, 0, sharedSecret.Length);

        // Generate nonce and AAD
        byte[] nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        byte[] aad = "test-aad"u8.ToArray();

        // Derive AES key using HKDF
        byte[] salt = SHA256.HashData(ctKem);
        byte[] contextBytes = Encoding.UTF8.GetBytes(context);
        byte[] aadLengthBytes = BitConverter.IsLittleEndian
            ? [(byte)(aad.Length >> 24), (byte)(aad.Length >> 16), (byte)(aad.Length >> 8), (byte)aad.Length]
            : BitConverter.GetBytes(aad.Length);

        byte[] info = new byte[contextBytes.Length + 4 + aad.Length];
        contextBytes.CopyTo(info, 0);
        aadLengthBytes.CopyTo(info, contextBytes.Length);
        aad.CopyTo(info.AsSpan(contextBytes.Length + 4));

        byte[] aesKey = HKDF.DeriveKey(HashAlgorithmName.SHA512, sharedSecret, 32, salt, info);

        // AES-GCM encryption
        byte[] ciphertextOnly = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (var aesGcm = new AesGcm(aesKey, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertextOnly, tag, aad);
        }

        // Append tag to ciphertext
        byte[] ciphertext = new byte[ciphertextOnly.Length + tag.Length];
        ciphertextOnly.CopyTo(ciphertext, 0);
        tag.CopyTo(ciphertext, ciphertextOnly.Length);

        // Build algorithm suite
        var algorithms = new AlgorithmSuite
        {
            Kem = "ML-KEM-768",
            Sig = "ML-DSA-65",
            Aead = "AES-256-GCM",
            Kdf = "HKDF-SHA-512"
        };

        // Build transcript
        byte[] version = [1];
        byte[] algsCiphersuite = Encoding.UTF8.GetBytes(algorithms.ToCiphersuiteString());
        byte[] serverSigPkBytes = serverSigPk.GetEncoded();

        int totalLength = 1 + algsCiphersuite.Length + contextBytes.Length +
                         ctKem.Length + nonce.Length + aad.Length +
                         ciphertext.Length + serverSigPkBytes.Length;

        byte[] transcript = new byte[totalLength];
        int offset = 0;

        transcript[offset++] = version[0];
        algsCiphersuite.CopyTo(transcript.AsSpan(offset));
        offset += algsCiphersuite.Length;
        contextBytes.CopyTo(transcript.AsSpan(offset));
        offset += contextBytes.Length;
        ctKem.CopyTo(transcript.AsSpan(offset));
        offset += ctKem.Length;
        nonce.CopyTo(transcript.AsSpan(offset));
        offset += nonce.Length;
        aad.CopyTo(transcript.AsSpan(offset));
        offset += aad.Length;
        ciphertext.CopyTo(transcript.AsSpan(offset));
        offset += ciphertext.Length;
        serverSigPkBytes.CopyTo(transcript.AsSpan(offset));

        // Sign the transcript
        var signer = new MLDsaSigner(sigParams, true);
        signer.Init(forSigning: true, serverSigSk);
        signer.BlockUpdate(transcript, 0, transcript.Length);
        byte[] signature = signer.GenerateSignature();

        return new EncryptedPayload
        {
            Version = 1,
            Algorithms = algorithms,
            CtKem = Base64Url.Encode(ctKem),
            Nonce = Base64Url.Encode(nonce),
            Aad = Base64Url.Encode(aad),
            Ciphertext = Base64Url.Encode(ciphertext),
            Signature = Base64Url.Encode(signature),
            ServerSigPk = Base64Url.Encode(serverSigPkBytes)
        };
    }
}
