# Security Policy

The security of the VaultSandbox Client SDK is our top priority. We appreciate the community's efforts to responsibly disclose vulnerabilities. This document outlines our security policy and procedures for reporting security concerns.

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.5.x   | :white_check_mark: |
| < 0.5   | :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability within the VaultSandbox Client SDK, please report it to us as quickly as possible. We kindly request that you do not disclose it publicly until we have had a chance to address it.

**Please do not report security vulnerabilities through public GitHub issues.**

**How to Report:**
Please send an email to `security@vaultsandbox.com`.

In your report, please include:

- A clear and concise description of the vulnerability.
- Steps to reproduce the vulnerability.
- The version(s) of VaultSandbox Client SDK affected.
- Any potential impact of the vulnerability.
- Your contact information, if you wish to be credited.

## Our Commitment

We are committed to:

- Acknowledging receipt of your vulnerability report within 48 hours.
- Providing an initial assessment within 7 days.
- Targeting resolution within 30 days for critical issues.
- Keeping you informed of our progress during the remediation process.
- Crediting you for your responsible disclosure, if you desire.

## Security Model

VaultSandbox uses a zero-knowledge architecture:

- **ML-KEM-768 (Kyber):** Post-quantum key encapsulation for email encryption
- **AES-256-GCM:** Symmetric encryption for email content
- **ML-DSA-65 (Dilithium):** Post-quantum signatures for integrity verification
- **HKDF-SHA-512:** Key derivation

### Key Principles

1.  **Gateway-side encryption:** The gateway receives emails via SMTP, encrypts them with your public key, and stores only ciphertext. Only your client SDK can decrypt.
2.  **Encrypted at rest:** Once encrypted, the gateway and any backend only handle ciphertext.
3.  **Signature verification before decryption:** All data is verified before decryption to prevent tampering.
4.  **Keys stay local:** Private keys never leave your application.

### Threat Model

This SDK protects against:

- Eavesdropping on stored emails (encryption)
- Data tampering (signatures)
- Future quantum attacks (post-quantum algorithms)
- Server compromise (zero-knowledge design)

This SDK does NOT protect against:

- Compromised client application
- Key theft from your environment
- Denial of service attacks

## Security Best Practices

When using this SDK:

1.  **Protect your API keys:** Use environment variables, never commit to source control
2.  **Handle exported inboxes carefully:** They contain private keys - treat as secrets
3.  **Monitor for `SignatureVerificationException`:** This may indicate tampering or MITM attacks
4.  **Keep dependencies updated:** Run `dotnet list package --vulnerable` regularly

## Acknowledgments

We appreciate responsible disclosure. Security researchers who report valid vulnerabilities will be acknowledged here (with permission).

Thank you for helping to keep the VaultSandbox Client SDK secure.
