using System.Security.Cryptography;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// AES-256-GCM authenticated encryption service.
/// </summary>
internal sealed class AesGcmService
{
    private const int KeySize = 32;   // 256 bits
    private const int NonceSize = 12; // 96 bits
    private const int TagSize = 16;   // 128 bits

    /// <summary>
    /// Decrypts AES-256-GCM ciphertext.
    /// </summary>
    /// <param name="key">32-byte AES key.</param>
    /// <param name="nonce">12-byte nonce.</param>
    /// <param name="ciphertext">Ciphertext with appended authentication tag.</param>
    /// <param name="aad">Additional authenticated data.</param>
    /// <returns>Decrypted plaintext.</returns>
    /// <exception cref="DecryptionException">Thrown when decryption fails.</exception>
    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> aad)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes", nameof(key));

        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

        if (ciphertext.Length < TagSize)
            throw new ArgumentException($"Ciphertext must include {TagSize}-byte tag", nameof(ciphertext));

        try
        {
            // Split ciphertext and tag
            // Tag is appended to the end of ciphertext
            var encryptedData = ciphertext[..^TagSize];
            var tag = ciphertext[^TagSize..];

            byte[] plaintext = new byte[encryptedData.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, encryptedData, tag, plaintext, aad);

            return plaintext;
        }
        catch (CryptographicException ex)
        {
            throw new DecryptionException("AES-GCM decryption failed. Data may be corrupted or tampered.", ex);
        }
    }
}
