using System.Security.Cryptography;
using System.Text;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// HKDF-SHA-512 key derivation service.
/// </summary>
internal sealed class HkdfService
{
    private const string Context = "vaultsandbox:email:v1";

    /// <summary>
    /// Derives a 256-bit AES key from the shared secret.
    /// </summary>
    /// <param name="sharedSecret">The shared secret from KEM decapsulation.</param>
    /// <param name="ctKem">The KEM ciphertext (used to derive salt).</param>
    /// <param name="aad">Additional authenticated data.</param>
    /// <returns>32-byte AES-256 key.</returns>
    public byte[] DeriveKey(ReadOnlySpan<byte> sharedSecret, ReadOnlySpan<byte> ctKem, ReadOnlySpan<byte> aad)
    {
        // Salt = SHA-256(ct_kem)
        byte[] salt = SHA256.HashData(ctKem);

        // Info = context || aad_length(4 bytes, big-endian) || aad
        byte[] contextBytes = Encoding.UTF8.GetBytes(Context);
        byte[] aadLength = BitConverter.IsLittleEndian
            ? [(byte)(aad.Length >> 24), (byte)(aad.Length >> 16), (byte)(aad.Length >> 8), (byte)aad.Length]
            : BitConverter.GetBytes(aad.Length);

        byte[] info = new byte[contextBytes.Length + 4 + aad.Length];
        contextBytes.CopyTo(info, 0);
        aadLength.CopyTo(info, contextBytes.Length);
        aad.CopyTo(info.AsSpan(contextBytes.Length + 4));

        // Derive 32-byte key using HKDF-SHA-512
        return HKDF.DeriveKey(
            HashAlgorithmName.SHA512,
            sharedSecret.ToArray(),
            outputLength: 32,
            salt: salt,
            info: info
        );
    }
}
