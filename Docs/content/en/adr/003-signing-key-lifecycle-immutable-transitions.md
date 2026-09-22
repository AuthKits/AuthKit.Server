[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./002-encrypt-keystore-at-rest.md) | [Next](./004-token-key-bindings-domain.md)

# [ADR-003] Model Signing Key Lifecycle As Immutable State Transitions

*2026-08* | Status: accepted

**Tag:** #adr_003

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement.Entity

## Context

A signing key moves through a lifecycle: it is generated, becomes valid for signing, may be rotated, expires, and can be revoked. Many parts of the system need to ask a single, reliable question — "is this key allowed to sign right now?" — without reimplementing lifecycle rules.

## Problem

If lifecycle state lives only in storage or is mutated in place by services, the rules for activation, expiration, and revocation get duplicated and drift. Mutable entities also make concurrent key operations and auditing harder to reason about.

## Decision

The signing key is modeled as an immutable `SigningKey` record. Lifecycle changes - `Activate`, `Revoke`, `Expire` - retur new instance via `with` expressions rather than mutating state, and a single `IsValidForSigning(now)` predicate evaluates activation, revocation, and expiration against supplied timestamp.

### SigningKey

**Responsibilities:**

- Carry the key identifier, public key (PEM), encrypted private material, algorithm, and lifecycle timestamps.
- Expose `Activate`, `Revoke`, and `Expire` as pure transitions that produce a new record.
- Provide `IsValidForSigning(DateTime now)` as the authoritative validity check.

### Design Rationale

- Immutability makes transitions easy to test and free of hidden side effects.
- One predicate for validity prevents lifecycle logic from being copied across the store, host, and bindings.
- Timestamp based evaluation keeps the rule deterministic and independent of wall clock assumptions inside the entity.

## Rejected

- A mutable entity with public setters changed directly by services.
- Storing lifecycle state only in the database and recomputing rules in each consumer.
- Embedding activation/expiration/revocation logic inside `JwtKeyStore` instead of the entity.

## Consequences

Lifecycle behavior is centralized and consistent, and transitions are auditable as value copies. The cost is richer record shape and the discipline of always using the returned instance rather than the original.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store that consumes the lifecycle
- [ADR-005](./005-public-keys-via-jwks.md) - JWKS excludes revoked keys using this state

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./002-encrypt-keystore-at-rest.md) | [Next](./004-token-key-bindings-domain.md)
