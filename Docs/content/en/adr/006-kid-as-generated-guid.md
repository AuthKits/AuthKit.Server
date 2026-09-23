[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./005-public-keys-via-jwks.md) | [Next](./007-default-signing-algorithm-rsa-4096.md)

# [ADR-006] Derive The JWT Key Identifier As Generated GUID

*2026-08* | Status: accepted

**Tag:** #adr_006

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Context

Every signing key needs stable identifier that appears as the JWT `kid` header, indexes the in-memory key dictionary, and links persistence records and public JWK entries. The generator creates keys with no external identifier assigned.

## Problem

If the identifier is meaningful (eg. encodes the public key or sequence), it can break when a key is rotated or its material regenerated, and it can leak internal structure. The system needs one identifier that is stable for the life of the key across all representations.

## Decision

The key generator assigns each new key `kid` produced by `Guid.NewGuid().ToString("N")`. That same value is used as the JWT `kid`, the `ConcurrentDictionary` key in `JwtKeyStore`, the `KeyMetadata.Kid`, and the JWKS `kid` field, so all representations are anchored to one identifier.

### Design Rationale

- A random GUID is unique, opaque, and stable for the key's lifetime regardless of rotation or re-export.
- Reusing one identifier everywhere removes translation logic between persistence, memory, JWTs, and JWKS.
- No internal structure is exposed to relying parties.

## Rejected

- Sequential or meaningful identifiers that reveal ordering or internals.
- Deriving the `kid` from a hash of the public key, which complicates rotation and re-exports.
- Letting callers or the host assign the `kid` externally.

## Consequences

Key identity is consistent and collision free across the system. The cost is that the `kid` carries no human-readable meaning, so debugging relies on metadata rather than the identifier itself.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store indexing by `kid`
- [ADR-005](./005-public-keys-via-jwks.md) - `kid` in the published key set
- [ADR-007](./007-default-signing-algorithm-rsa-4096.md) - algorithm paired with the identifier

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./005-public-keys-via-jwks.md) | [Next](./007-default-signing-algorithm-rsa-4096.md)
