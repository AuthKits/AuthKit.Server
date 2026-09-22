[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./003-signing-key-lifecycle-immutable-transitions.md) | [Next](./005-public-keys-via-jwks.md)

# [ADR-004] Treat Developer Token To Signing Key Bindings As Core Domain

*2026-08* | Status: accepted

**Tag:** #adr_004

**Date:** 2026-08-26

**Scope:** AuthKit.Core.TokenKeyBindings

## Context

Developer tokens must be associated with the specific RSA signing key (and its public key) used to sign their JWTs, so that verification and rotation can be tracked per token. This association is domain concept, not an implementation detail of single endpoint.

## Problem

Without first class binding concept, the mapping between tokens and signing keys is scattered across hosts and key material, making rotation, public key updates, and revocation hard to trace and inconsistent across API surfaces.

## Decision

Token to key bindings are core domain: the `TokenKeyBinding` record captures `TokenId`, `SigningKeyId`, the public key, `BoundAt` timestamp, and `Revoked` flag. `IKeyBindingService` applies domain behavior (`CreateBindingAsync`, `RebindAsync`, `UpdatePublicKeyAsync`, `RevokeAsync`, `ListBindingsAsync`, `GetBindingAsync`), and `IKeyBindingRepository` is pure persistence boundary.

### TokenKeyBinding

**Responsibilities:**

- Represent the immutable by copy association between developer token and signing key.
- Expose `Rebind`, `UpdatePublicKey`, and `Revoke` as transitions returning a new record with refreshed `BoundAt`.

### IKeyBindingService

**Responsibilities:**

- Orchestrate binding operations through the repository.
- Invoke the entity's transitions rather than mutating binding state directly.

### IKeyBindingRepository

**Responsibilities:**

- Persist, load, update, and list bindings.
- Stay free of cryptographic and domain behavior.

### Design Rationale

- A dedicated domain keeps per token key provenance explicit and supports rotation without breaking verification.
- Separating service (behavior) from repository (persistence) mirrors the key management design and stays testable.

## Rejected

- Encoding the token -> key mapping implicitly inside the key store.
- Letting hosts manage bindings through ad hoc database access.
- Mutating binding fields directly from the service instead of using entity transitions.

## Consequences

Token key provenance, rotation, and revocation are consistent and observable. The cost is maintaining second core domain alongside key management and keeping the repository boundary clean.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - signing keys the bindings reference
- [ADR-009](./009-dynamic-plugin-discovery.md) - plugins consume bindings through this domain
- [ADR-012](./012-token-key-bindings-persisted-in-marten.md) - persistence of bindings

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./003-signing-key-lifecycle-immutable-transitions.md) | [Next](./005-public-keys-via-jwks.md)
