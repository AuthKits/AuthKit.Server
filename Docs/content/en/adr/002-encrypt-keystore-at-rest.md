[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./001-centralize-signing-key-management.md) | [Next](./003-signing-key-lifecycle-immutable-transitions.md)

# [ADR-002] Encrypt Persisted Keystore Material At Rest Through A Pluggable Encryptor

*2026-08* | Status: accepted

**Tag:** #adr_002

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Context

The key store persists sensitive RSA private key material so that signing keys survive restarts. The repository boundary (`IKeyStoreRepository`) is intentionally pure persistence sink - it stores and returns opaque bytes without understanding their content.

## Problem

Private key material must never be written to disk in plaintext. At the same time, the persistence layer must remain unaware of cryptography so it can be swapped (file, blob, database) without touching security logic. The system also needs a single, replaceable place to change the encryption algorithm if the threat model changes.

## Decision

All keystore bytes are encrypted before persistence and decrypted after load through the `IKeyEncryptor` boundary. The default implementation (`AesKeyEncryptor`) uses AES-256-CBC with 256-bit master key supplied at construction, generates cryptographically random IV per encryption call, and prefixes the IV to the ciphertext so it can be recovered on decryption.

### IKeyEncryptor

**Responsibilities:**

- Encrypt UTF-8 plaintext into an opaque byte blob (`Encrypt`).
- Reverse the operation and return plaintext (`Decrypt`).
- Remain stateless and independent of the persistence repository.

### Design Rationale

- Keeping encryption in dedicated, injectable boundary lets the algorithm be changed (e.g. to AES-GCM) without modifying the store or repository.
- A per call random IV avoids key/IV reuse and keeps the on disk format non deterministic.
- The repository stays dumb byte store, so storage backends are interchangeable.

## Rejected

- Persisting keys in plaintext or relying on filesystem permissions alone.
- Letting `IKeyStoreRepository` perform encryption or decryption.
- A fixed, embedded key compiled into the assembly.
- Reusing single static IV across all encryptions.

## Consequences

Key material is confidential at rest and the encryption algorithm is isolated behind a contract. The documentation explicitly notes that AES-CBC provides confidentiality without authenticated integrity, so the encrypted blob must be protected from tampering by another mechanism or migrated to an authenticated mode.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store abstraction owning the contract
- [ADR-011](./011-keystore-persisted-as-singleton-marten-document.md) - where the encrypted bytes are persisted

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./001-centralize-signing-key-management.md) | [Next](./003-signing-key-lifecycle-immutable-transitions.md)
