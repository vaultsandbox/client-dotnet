using System.Security.Cryptography;
using System.Text;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// Composite cryptographic provider implementing the full decryption flow.
/// Conforms to VaultSandbox spec Sections 5, 7, and 8.
/// </summary>
internal sealed class CryptoProvider : ICryptoProvider
{
    private const string Context = "vaultsandbox:email:v1";

    // Per spec Section 3.1: Required algorithm suite
    private const string ExpectedKem = "ML-KEM-768";
    private const string ExpectedSig = "ML-DSA-65";
    private const string ExpectedAead = "AES-256-GCM";
    private const string ExpectedKdf = "HKDF-SHA-512";

    // Per spec Section 5.3 and Appendix B: Size constraints
    private const int CtKemSize = 1088;
    private const int NonceSize = 12;
    private const int SignatureSize = 3309;
    private const int ServerSigPkSize = 1952;

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
    /// Decryption flow per spec Section 8.1:
    /// 1. Parse payload (done by caller)
    /// 2. Validate version
    /// 3. Validate algorithms
    /// 4. Validate sizes
    /// 5. Verify server key
    /// 6. Verify signature (BEFORE decryption)
    /// 7. Decapsulate
    /// 8. Derive AES key
    /// 9. Decrypt
    /// </remarks>
    public byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> secretKey, string expectedServerSigPk)
    {
        // ═══════════════════════════════════════════════════════════════════
        // STEP 2: VALIDATE VERSION (per spec Section 8.1)
        // ═══════════════════════════════════════════════════════════════════
        if (payload.Version != 1)
        {
            throw new DecryptionException($"Unsupported payload version: {payload.Version} (expected 1)");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STEP 3: VALIDATE ALGORITHMS (per spec Section 8.1)
        // ═══════════════════════════════════════════════════════════════════
        ValidateAlgorithms(payload.Algorithms);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 5: VERIFY SERVER KEY (SECURITY CRITICAL - uses constant-time comparison)
        // Prevents key substitution attacks where an attacker replaces the
        // server's signing key with their own.
        // Per spec Section 8.2: MUST use constant-time comparison.
        // ═══════════════════════════════════════════════════════════════════
        byte[] expectedServerSigPkBytes = Base64Url.Decode(expectedServerSigPk);
        byte[] payloadServerSigPkBytes = Base64Url.Decode(payload.ServerSigPk);

        if (!CryptographicOperations.FixedTimeEquals(expectedServerSigPkBytes, payloadServerSigPkBytes))
        {
            throw new ServerKeyMismatchException(expectedServerSigPk, payload.ServerSigPk);
        }

        // Decode all base64url fields
        byte[] ctKem = Base64Url.Decode(payload.CtKem);
        byte[] nonce = Base64Url.Decode(payload.Nonce);
        byte[] aad = Base64Url.Decode(payload.Aad);
        byte[] ciphertext = Base64Url.Decode(payload.Ciphertext);
        byte[] signature = Base64Url.Decode(payload.Signature);
        byte[] serverSigPk = payloadServerSigPkBytes;

        // ═══════════════════════════════════════════════════════════════════
        // STEP 4: VALIDATE SIZES (per spec Section 5.3)
        // ═══════════════════════════════════════════════════════════════════
        ValidateSizes(ctKem, nonce, signature, serverSigPk);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 6: VERIFY SIGNATURE (SECURITY CRITICAL - BEFORE decryption)
        // Per spec Section 8.2: Signature verification MUST occur before decryption.
        // ═══════════════════════════════════════════════════════════════════
        byte[] transcript = BuildTranscript(payload, ctKem, nonce, aad, ciphertext, serverSigPk);
        _mlDsaService.VerifyOrThrow(signature, transcript, serverSigPk);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 7: KEM DECAPSULATION
        // ═══════════════════════════════════════════════════════════════════
        byte[] sharedSecret = _mlKemService.Decapsulate(ctKem, secretKey);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 8: KEY DERIVATION (HKDF-SHA-512)
        // ═══════════════════════════════════════════════════════════════════
        byte[] aesKey = _hkdfService.DeriveKey(sharedSecret, ctKem, aad);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 9: AES-256-GCM DECRYPTION
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

    /// <summary>
    /// Validates that all algorithm identifiers match expected values.
    /// Per spec Section 3.1 and 8.1: Implementations MUST reject payloads with different algorithms.
    /// </summary>
    private static void ValidateAlgorithms(AlgorithmSuite algs)
    {
        if (algs.Kem != ExpectedKem)
            throw new DecryptionException(
                $"Unsupported KEM algorithm: {algs.Kem} (expected {ExpectedKem})");

        if (algs.Sig != ExpectedSig)
            throw new DecryptionException(
                $"Unsupported signature algorithm: {algs.Sig} (expected {ExpectedSig})");

        if (algs.Aead != ExpectedAead)
            throw new DecryptionException(
                $"Unsupported AEAD algorithm: {algs.Aead} (expected {ExpectedAead})");

        if (algs.Kdf != ExpectedKdf)
            throw new DecryptionException(
                $"Unsupported KDF algorithm: {algs.Kdf} (expected {ExpectedKdf})");
    }

    /// <summary>
    /// Validates decoded field sizes per spec Section 5.3.
    /// </summary>
    private static void ValidateSizes(
        ReadOnlySpan<byte> ctKem,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> signature,
        ReadOnlySpan<byte> serverSigPk)
    {
        if (ctKem.Length != CtKemSize)
            throw new DecryptionException(
                $"Invalid ct_kem size: {ctKem.Length} bytes (expected {CtKemSize})");

        if (nonce.Length != NonceSize)
            throw new DecryptionException(
                $"Invalid nonce size: {nonce.Length} bytes (expected {NonceSize})");

        if (signature.Length != SignatureSize)
            throw new DecryptionException(
                $"Invalid signature size: {signature.Length} bytes (expected {SignatureSize})");

        if (serverSigPk.Length != ServerSigPkSize)
            throw new DecryptionException(
                $"Invalid server_sig_pk size: {serverSigPk.Length} bytes (expected {ServerSigPkSize})");
    }
}
