[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./004-token-key-bindings-domain.md) | [Next](./006-kid-as-generated-guid.md)

# [ADR-005] Publish Public Keys Through JWKS Exposing Only Non Revoked Keys

*2026-08* | Status: accepted

**Tag:** #adr_005

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Context

Relying parties verify JWT signatures by fetching the issuer's public keys. The key store already holds each key's RSA modulus and exponent plus its lifecycle metadata, and it keeps an in-memory view of all loaded keys.

## Problem

Consumers need standard, cache friendly way to discover valid public keys. If revoked or expired keys are published, or if the response is recomputed on every request without invalidation, verifiers may accept bad signatures or the endpoint wastes CPU reconstructing the key set.

## Decision

The key store exposes public keys via `GetPublicJwks()` as `PublicJwkDto` instances in JWKS compatible shape (`kty=RSA`, `use=sig`, `kid`, `alg`, `n`, `e`). Only keys whose `Revoked` metadata is false are included, and the result is cached, rebuilt only when the key set changes (rotation or revocation).

### PublicJwkDto

**Responsibilities:**

- Carry the JWKS fields needed by external verifiers.
- Remain read only projection of the in-memory key entry.

### Design Rationale

- JWKS is an industry standard discovery format, so off the shelf verifiers integrate without custom code.
- Excluding revoked keys prevents accepting signatures from retired keys.
- Caching with explicit invalidation balances performance against freshness after rotation.

## Rejected

- Publishing revoked or expired keys in the JWKS response.
- Regenerating the key set on every request without cache.
- Inventing custom public key discovery format instead of JWKS.

## Consequences

External verifiers get correct, standard key set and the endpoint stays efficient. The cost is explicitly invalidating the cache on every lifecycle change and keeping the projection in sync with key metadata.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store exposing the JWKS
- [ADR-003](./003-signing-key-lifecycle-immutable-transitions.md) - lifecycle drives revocation filtering
- [ADR-006](./006-kid-as-generated-guid.md) - `kid` values published in the key set

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./004-token-key-bindings-domain.md) | [Next](./006-kid-as-generated-guid.md)
