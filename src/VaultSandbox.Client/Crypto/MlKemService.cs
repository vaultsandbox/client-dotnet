using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// ML-KEM-768 (Kyber768) key encapsulation service.
/// </summary>
internal sealed class MlKemService
{
    private const int PublicKeySize = 1184;
    private const int SecretKeySize = 2400;
    private const int SharedSecretSize = 32;

    private readonly MLKemParameters _parameters = MLKemParameters.ml_kem_768;

    /// <summary>
    /// Generates a new ML-KEM-768 keypair.
    /// </summary>
    /// <returns>Tuple of (publicKey, secretKey).</returns>
    public (byte[] PublicKey, byte[] SecretKey) GenerateKeypair()
    {
        var keyGenParams = new MLKemKeyGenerationParameters(
            new SecureRandom(),
            _parameters
        );

        var keyPairGenerator = new MLKemKeyPairGenerator();
        keyPairGenerator.Init(keyGenParams);

        AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();

        var publicKey = (MLKemPublicKeyParameters)keyPair.Public;
        var secretKey = (MLKemPrivateKeyParameters)keyPair.Private;

        return (publicKey.GetEncoded(), secretKey.GetEncoded());
    }

    /// <summary>
    /// Decapsulates the KEM ciphertext to recover the shared secret.
    /// </summary>
    /// <param name="ctKem">KEM ciphertext from the server.</param>
    /// <param name="secretKey">Client's secret key (2400 bytes).</param>
    /// <returns>32-byte shared secret.</returns>
    /// <exception cref="DecryptionException">Thrown when decapsulation fails.</exception>
    public byte[] Decapsulate(ReadOnlySpan<byte> ctKem, ReadOnlySpan<byte> secretKey)
    {
        if (secretKey.Length != SecretKeySize)
            throw new ArgumentException($"Secret key must be {SecretKeySize} bytes", nameof(secretKey));

        try
        {
            var privateKeyParams = MLKemPrivateKeyParameters.FromEncoding(_parameters, secretKey.ToArray());

            var decapsulator = new MLKemDecapsulator(_parameters);
            decapsulator.Init(privateKeyParams);

            byte[] sharedSecret = new byte[decapsulator.SecretLength];
            decapsulator.Decapsulate(ctKem.ToArray(), 0, ctKem.Length, sharedSecret, 0, sharedSecret.Length);

            if (sharedSecret.Length != SharedSecretSize)
                throw new DecryptionException($"Unexpected shared secret size: {sharedSecret.Length}");

            return sharedSecret;
        }
        catch (Exception ex) when (ex is not DecryptionException)
        {
            throw new DecryptionException("ML-KEM-768 decapsulation failed", ex);
        }
    }

    /// <summary>
    /// Extracts the public key from a secret key.
    /// In ML-KEM-768, the secret key structure is:
    /// privateKey = cpaPrivateKey(1152) || cpaPublicKey(1184) || h(32) || z(32)
    /// </summary>
    /// <param name="secretKey">Secret key (2400 bytes).</param>
    /// <returns>Public key (1184 bytes).</returns>
    public byte[] ExtractPublicKey(ReadOnlySpan<byte> secretKey)
    {
        if (secretKey.Length != SecretKeySize)
            throw new ArgumentException($"Secret key must be {SecretKeySize} bytes", nameof(secretKey));

        // Public key starts at offset 1152 and is 1184 bytes
        return secretKey.Slice(1152, PublicKeySize).ToArray();
    }
}
