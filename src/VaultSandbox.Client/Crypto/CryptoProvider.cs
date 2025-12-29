using System.Text;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// Composite cryptographic provider implementing the full decryption flow.
/// </summary>
internal sealed class CryptoProvider : ICryptoProvider
{
    private const string Context = "vaultsandbox:email:v1";

    private readonly MlKemService _mlKemService;
    private readonly MlDsaService _mlDsaService;
    private readonly AesGcmService _aesGcmService;
    private readonly HkdfService _hkdfService;

    public CryptoProvider()
    {
        _mlKemService = new MlKemService();
        _mlDsaService = new MlDsaService();
        _aesGcmService = new AesGcmService();
        _hkdfService = new HkdfService();
    }

    /// <inheritdoc />
    public MlKemKeyPair GenerateKeyPair()
    {
        var (publicKey, secretKey) = _mlKemService.GenerateKeypair();
        return new MlKemKeyPair
        {
            PublicKey = publicKey,
            SecretKey = secretKey
        };
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(EncryptedPayload payload, byte[] secretKey, string expectedServerSigPk, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Decrypt(payload, secretKey, expectedServerSigPk));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Decryption flow:
    /// 1. VALIDATE SERVER SIGNING KEY (security-critical - must be FIRST)
    /// 2. VERIFY SIGNATURE
    /// 3. KEM decapsulation
    /// 4. HKDF key derivation
    /// 5. AES-GCM decryption
    /// </remarks>
    public byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> secretKey, string expectedServerSigPk)
    {
        // ═══════════════════════════════════════════════════════════════════
        // STEP 0: VALIDATE SERVER SIGNING KEY (MUST BE FIRST - SECURITY CRITICAL)
        // Prevents key substitution attacks where an attacker replaces the
        // server's signing key with their own.
        // ═══════════════════════════════════════════════════════════════════
        if (!string.Equals(payload.ServerSigPk, expectedServerSigPk, StringComparison.Ordinal))
        {
            throw new ServerKeyMismatchException(expectedServerSigPk, payload.ServerSigPk);
        }

        // Decode all base64url fields
        byte[] ctKem = Base64Url.Decode(payload.CtKem);
        byte[] nonce = Base64Url.Decode(payload.Nonce);
        byte[] aad = Base64Url.Decode(payload.Aad);
        byte[] ciphertext = Base64Url.Decode(payload.Ciphertext);
        byte[] signature = Base64Url.Decode(payload.Signature);
        byte[] serverSigPk = Base64Url.Decode(payload.ServerSigPk);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 1: VERIFY SIGNATURE (SECURITY CRITICAL)
        // ═══════════════════════════════════════════════════════════════════
        byte[] transcript = BuildTranscript(payload, ctKem, nonce, aad, ciphertext, serverSigPk);
        _mlDsaService.VerifyOrThrow(signature, transcript, serverSigPk);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 2: KEM DECAPSULATION
        // ═══════════════════════════════════════════════════════════════════
        byte[] sharedSecret = _mlKemService.Decapsulate(ctKem, secretKey);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 3: KEY DERIVATION (HKDF-SHA-512)
        // ═══════════════════════════════════════════════════════════════════
        byte[] aesKey = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 4: AES-256-GCM DECRYPTION
        // ═══════════════════════════════════════════════════════════════════
        return _aesGcmService.Decrypt(aesKey, nonce, ciphertext, aad);
    }

    /// <summary>
    /// Builds the transcript for signature verification.
    /// Must match the server's construction exactly.
    /// </summary>
    private static byte[] BuildTranscript(
        EncryptedPayload payload,
        ReadOnlySpan<byte> ctKem,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> serverSigPk)
    {
        // transcript = version (1 byte)
        //            || algs_ciphersuite (string)
        //            || context (string)
        //            || ct_kem (bytes)
        //            || nonce (bytes)
        //            || aad (bytes)
        //            || ciphertext (bytes)
        //            || server_sig_pk (bytes)

        byte[] versionBytes = [(byte)payload.Version];
        byte[] algsCiphersuite = Encoding.UTF8.GetBytes(payload.Algorithms.ToCiphersuiteString());
        byte[] contextBytes = Encoding.UTF8.GetBytes(Context);

        int totalLength = 1 + algsCiphersuite.Length + contextBytes.Length +
                         ctKem.Length + nonce.Length + aad.Length +
                         ciphertext.Length + serverSigPk.Length;

        byte[] transcript = new byte[totalLength];
        int offset = 0;

        // Version (1 byte)
        transcript[offset++] = versionBytes[0];

        // Algs ciphersuite
        algsCiphersuite.CopyTo(transcript.AsSpan(offset));
        offset += algsCiphersuite.Length;

        // Context
        contextBytes.CopyTo(transcript.AsSpan(offset));
        offset += contextBytes.Length;

        // ct_kem
        ctKem.CopyTo(transcript.AsSpan(offset));
        offset += ctKem.Length;

        // nonce
        nonce.CopyTo(transcript.AsSpan(offset));
        offset += nonce.Length;

        // aad
        aad.CopyTo(transcript.AsSpan(offset));
        offset += aad.Length;

        // ciphertext
        ciphertext.CopyTo(transcript.AsSpan(offset));
        offset += ciphertext.Length;

        // server_sig_pk
        serverSigPk.CopyTo(transcript.AsSpan(offset));

        return transcript;
    }
}
