[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous]() | [Next](./002-encrypt-keystore-at-rest.md)

# [ADR-001] Centralize Signing Key Management Through A Core Key Store Abstraction

*2026-08* | Status: accepted

**Tag:** #adr_001

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Context

The AuthKit core owns the cryptographic material used to issue and verify JWTs. It exposes a small set of primitives - `IKeyGenerator`, `IKeyEncryptor`, `IJwtKeyStore`, and `IKeyStoreRepository` - that together cover key creation, at rest encryption, and persistence. The host layer (REST, gRPC, plugins) must be able to mint, rotate, and publish signing keys without taking on knowledge of how those keys are generated, encrypted, or stored.

## Problem

The host should not depend on concrete key storage formats, encryption algorithms, or generation parameters. Without central key management abstraction owned by the core, that knowledge leaks into the host and into individual endpoints: each consumer reinvents how keys are produced, how they are protected on disk, and how the active signing key is resolved. Renaming key, changing the encryption scheme, or swapping the storage backend then becomes cross cutting change with no single contract to anchor it.

## Decision

Signing key management is centralized behind core abstraction that forms a contract between key generation, encryption, and persistence on one side, and the host on the other. A signing key becomes a stable core entity (`SigningKey`) resolved through the key store, rather than a raw file or repository record known only to single endpoint.

### IKeyGenerator

**Responsibilities:**

- Produce new asymmetric key pairs for signing (`GenerateAsync`).
- Expose the algorithm and parameters used so the rest of the core can reason about key strength.
- Remain agnostic of storage and encryption concerns.

### IKeyEncryptor

**Responsibilities:**

- Encrypt raw key material for at rest protection (`EncryptAsync` / `DecryptAsync`).
- Keep the encryption algorithm (eg. AES) isolated from the store and generator.
- Never expose plaintext material outside the core boundary.

### IJwtKeyStore

**Responsibilities:**

- Resolve the active signing key and the set of published public keys (`GetActiveKeyAsync`, `GetPublicJwksAsync`).
- Coordinate the generator and encryptor so that persisted keys are always encrypted.
- Present keys to the host as `SigningKey` entities and public JWKs, hiding storage details.

### IKeyStoreRepository

**Responsibilities:**

- Persist and load the encrypted keystore record (`KeystoreOnDisk`, `KeyEntry`, `KeyMetadata`).
- Remain pure persistence boundary with no cryptographic or domain logic.

### Design Rationale

- A single core contract keeps cryptographic decisions testable, auditable, and swappable without touching the host.
- Separating generation, encryption, and storage lets each concern evolve independently (algorithm upgrade, storage backend change, rotation policy) behind stable interfaces.
- Exposing keys as domain entities and public JWKs prevents the host from coupling to on disk formats and reduces the risk of key handling mistakes.

## Rejected

- Letting the host generate, encrypt, and persist keys directly with ad hoc calls.
- Coupling signing key and encryption knowledge into concrete endpoint code.
- A single monolithic key service that merges generation, encryption, and storage in one class.
- Storing keys in plaintext or relying on host managed secrets rather than the core encryptor.

## Consequences

The core becomes the single, predictable owner of signing key lifecycle and it is easier to control key completeness, rotation, and algorithm consistency across the host. The cost is maintaining the core abstraction as a contract and updating it whenever new key management capability is introduced.

## Related

- [ADR-002](./002-encrypt-keystore-at-rest.md) - encryption of keystore bytes at rest
- [ADR-003](./003-signing-key-lifecycle-immutable-transitions.md) - signing key lifecycle transitions
- [ADR-005](./005-public-keys-via-jwks.md) - public key discovery via JWKS
- [ADR-006](./006-kid-as-generated-guid.md) - key identifier strategy
- [ADR-007](./007-default-signing-algorithm-rsa-4096.md) - default signing algorithm
- [ADR-011](./011-keystore-persisted-as-singleton-marten-document.md) - keystore persistence

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous]() | [Next](./002-encrypt-keystore-at-rest.md)
