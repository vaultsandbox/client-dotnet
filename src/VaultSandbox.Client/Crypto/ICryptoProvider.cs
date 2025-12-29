namespace VaultSandbox.Client.Crypto;

/// <summary>
/// Interface for cryptographic operations.
/// </summary>
public interface ICryptoProvider
{
    /// <summary>
    /// Generates a new ML-KEM-768 keypair.
    /// </summary>
    /// <returns>Keypair with public and secret keys.</returns>
    MlKemKeyPair GenerateKeyPair();

    /// <summary>
    /// Decrypts an encrypted payload synchronously.
    /// </summary>
    /// <param name="payload">The encrypted payload from the server.</param>
    /// <param name="secretKey">The client's ML-KEM-768 secret key.</param>
    /// <param name="expectedServerSigPk">The expected server signing public key (base64url-encoded) to validate against.</param>
    /// <returns>Decrypted plaintext bytes.</returns>
    /// <exception cref="Exceptions.ServerKeyMismatchException">If the payload's server signing key doesn't match the expected key (checked FIRST).</exception>
    /// <exception cref="Exceptions.SignatureVerificationException">If signature verification fails.</exception>
    /// <exception cref="Exceptions.DecryptionException">If decryption fails.</exception>
    byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> secretKey, string expectedServerSigPk);

    /// <summary>
    /// Decrypts an encrypted payload asynchronously.
    /// This is a convenience wrapper for async contexts.
    /// </summary>
    /// <param name="payload">The encrypted payload from the server.</param>
    /// <param name="secretKey">The client's ML-KEM-768 secret key.</param>
    /// <param name="expectedServerSigPk">The expected server signing public key (base64url-encoded) to validate against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Decrypted plaintext bytes.</returns>
    /// <exception cref="Exceptions.ServerKeyMismatchException">If the payload's server signing key doesn't match the expected key (checked FIRST).</exception>
    /// <exception cref="Exceptions.SignatureVerificationException">If signature verification fails.</exception>
    /// <exception cref="Exceptions.DecryptionException">If decryption fails.</exception>
    Task<byte[]> DecryptAsync(EncryptedPayload payload, byte[] secretKey, string expectedServerSigPk, CancellationToken ct = default);
}

/// <summary>
/// Represents an ML-KEM-768 keypair.
/// </summary>
public sealed record MlKemKeyPair
{
    /// <summary>
    /// Public key (1184 bytes).
    /// </summary>
    public required byte[] PublicKey { get; init; }

    /// <summary>
    /// Secret key (2400 bytes).
    /// </summary>
    public required byte[] SecretKey { get; init; }

    /// <summary>
    /// Base64url-encoded public key.
    /// </summary>
    public string PublicKeyB64 => Base64Url.Encode(PublicKey);

    /// <summary>
    /// Base64url-encoded secret key.
    /// </summary>
    public string SecretKeyB64 => Base64Url.Encode(SecretKey);
}
