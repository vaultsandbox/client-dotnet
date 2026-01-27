# Changelog

All notable changes to this project will be documented in this file.

The format is inspired by [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.9.1] - 2026-01-28

### Added

- Webhook and chaos engineering examples in README and integration tests

### Fixed

- Resource leak in PollingDeliveryStrategy
- Resource leak in SSE stream (HttpResponseMessage not disposed)
- ReDoS vulnerability in WaitForEmailOptions regex pattern
- Invalid regex patterns now throw descriptive ArgumentException
- Null guards for decryption operations
- 404 exception handling in ApiClient

### Changed

- Extracted channel creation helper in Inbox for better maintainability
- Added `volatile` modifier for thread safety in Inbox and SseDeliveryStrategy
- Improved CancellationToken propagation throughout Inbox operations

## [0.9.0] - 2026-01-22

### Added

- Chaos Engineering

## [0.8.5] - 2026-01-19

### Added

- Spam analysis support (Rspamd integration)
- Inbox creation option to enable/disable spam analysis
- Server capability detection for spam analysis

## [0.8.0] - 2026-01-16

### Added

- Webhooks support for inbox

## [0.7.0] - 2026-01-13

### Added

- Optional encryption support with `encryptionPolicy` option
- Optional email authentication feature

### Changed

- Updated ReverseDNS structure
- License changed from MIT to Apache 2.0

## [0.6.1] - 2026-01-11

### Removed

- `Auto` delivery strategy (use `Sse` or `Polling` explicitly)

### Changed

- Email sync now uses hash-based comparison for efficient change detection
- SSE reconnection triggers automatic inbox sync to catch missed emails
- Duplicate email events are now filtered locally

## [0.6.0] - 2026-01-04

### Breaking Changes

- Renamed `InboxExport.PublicKeyB64` and `SecretKeyB64` to `SecretKey` (public key is now derived from secret key)
- Added explicit JSON property names to `InboxExport` to conform to wire format

### Changed

- Server key comparison now uses constant-time comparison (`CryptographicOperations.FixedTimeEquals`)
- Decryption flow reorganized to match spec

### Added

- `GetEmailsMetadataOnlyAsync()` method for efficient email listing without body content
- `EmailMetadata` record with `Id`, `From`, `Subject`, `ReceivedAt`, `IsRead` properties
- `GetEmailsAsync()` now fetches full email content in a single request (no more N+1 queries)
- `InboxExport.Version` field (must be 1)
- Payload version validation before decryption
- Algorithm suite validation (KEM, Sig, AEAD, KDF) per spec Section 3.1
- Size validation for ct_kem, nonce, signature, and server_sig_pk per spec Section 5.3
- Strict Base64URL validation rejecting standard Base64 characters (+, /, =)
- Email address format validation on import (must contain exactly one @)
- Server signing public key size validation on import

## [0.5.1] - 2025-12-31

### Changed

- Standardized email authentication result structs to match wire format and other SDKs

### Added

- End-to-end integration tests for email authentication results using the test email API

## [0.5.0] - 2025-12-15

### Initial release

- Quantum-safe email testing SDK with ML-KEM-768 encryption
- Automatic keypair generation and management
- Support for both polling and real-time (SSE) email delivery
- Full email content access including attachments and headers
- Built-in SPF/DKIM/DMARC authentication validation
- Full C# support with strong typing
- Inbox import/export functionality for test reproducibility
- Comprehensive error handling with automatic retries
