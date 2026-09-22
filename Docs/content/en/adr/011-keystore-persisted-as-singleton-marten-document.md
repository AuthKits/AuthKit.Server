[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./010-plugin-loading-from-directory.md) | [Next](./012-token-key-bindings-persisted-in-marten.md)

# [ADR-011] Persist The Encrypted Keystore As Singleton Marten Document

*2026-08* | Status: accepted

**Tag:** #adr_011

**Date:** 2026-08-26

**Scope:** Host.KeyManagement.Repositories

## Context

ADR-001 defines `IKeyStoreRepository` as pure persistence sink, and ADR-002 establishes that only encrypted keystore bytes are ever handed to it. The host must decide where those encrypted bytes actually live.

## Problem

The encrypted keystore must be durable across restarts, but the repository must not take on cryptography, and the store must be addressable simply - there is exactly one keystore for the whole server.

## Decision

`KeyStoreRepository` persists the keystore as single Marten document with fixed id (`"singleton"`), via lightweight sessions. It stores only the encrypted payload (`KeystoreDocument.EncryptedData`); it never encrypts, decrypts, or interprets the content. On load it returns `Memory<byte>.Empty` when no document exists on save it inserts or updates the singleton document, copying the caller's bytes into fresh array first.

### KeyStoreRepository

**Responsibilities:**

- Load/save the encrypted keystore as one Marten document identified by constant id.
- Stay cryptography agnostic and use only lightweight sessions.

### Design Rationale

- A singleton document matches the "one keystore per server" model and avoids key/lookup management.
- Keeping the repository a dumb encrypted-byte store honors the boundary from ADR-001/ADR-002.
- Marten gives durable, transactional persistence that the rest of the host (Wolverine, plugins) already uses.

## Rejected

- A separate flat file or custom blob store outside Marten.
- Multiple keystore documents or per key documents.
- Letting the repository perform encryption or decryption.

## Consequences

Key material survives restarts in the same store as the rest of the host's data, and rotation (ADR-001) is just an upsert of the singleton. The cost is hard dependency on Marten/Postgres for key availability and the discipline that the repository never inspects the payload.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store abstraction persisted here
- [ADR-002](./002-encrypt-keystore-at-rest.md) - only encrypted bytes are stored
- [ADR-016](./016-marten-and-wolverine-infrastructure.md) - Marten as the host store

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./010-plugin-loading-from-directory.md) | [Next](./012-token-key-bindings-persisted-in-marten.md)
