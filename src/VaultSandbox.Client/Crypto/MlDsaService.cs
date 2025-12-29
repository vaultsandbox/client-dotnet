using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using VaultSandbox.Client.Exceptions;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// ML-DSA-65 (Dilithium3) signature verification service.
/// </summary>
internal sealed class MlDsaService
{
    private const int PublicKeySize = 1952;

    private readonly MLDsaParameters _parameters = MLDsaParameters.ml_dsa_65;

    /// <summary>
    /// Verifies an ML-DSA-65 signature.
    /// </summary>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="message">The signed message (transcript).</param>
    /// <param name="publicKey">The signer's public key (1952 bytes).</param>
    /// <returns>True if signature is valid.</returns>
    /// <exception cref="SignatureVerificationException">Thrown when verification fails.</exception>
    public bool Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> publicKey)
    {
        if (publicKey.Length != PublicKeySize)
            throw new ArgumentException($"Public key must be {PublicKeySize} bytes", nameof(publicKey));

        try
        {
            var publicKeyParams = MLDsaPublicKeyParameters.FromEncoding(_parameters, publicKey.ToArray());

            var verifier = new MLDsaSigner(_parameters, false);
            verifier.Init(forSigning: false, publicKeyParams);
            verifier.BlockUpdate(message.ToArray(), 0, message.Length);

            return verifier.VerifySignature(signature.ToArray());
        }
        catch (Exception ex)
        {
            throw new SignatureVerificationException("ML-DSA-65 signature verification failed", ex);
        }
    }

    /// <summary>
    /// Verifies signature and throws if invalid.
    /// </summary>
    /// <exception cref="SignatureVerificationException">Thrown when signature is invalid.</exception>
    public void VerifyOrThrow(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> publicKey)
    {
        if (!Verify(signature, message, publicKey))
        {
            throw new SignatureVerificationException(
                "Signature verification failed. The data may have been tampered with.");
        }
    }
}
