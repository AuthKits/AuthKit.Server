[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./006-kid-as-generated-guid.md) | [Next](./008-standardized-error-response.md)

# [ADR-007] Default Signing Algorithm Is RSA-4096 With RS256

*2026-08* | Status: accepted

**Tag:** #adr_007

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Context

The key generator and key store must agree on the algorithm and key strength used to produce and verify JWTs. `IKeyGenerator.Generate` takes configurable `rsaBits`, and `JwtKeyStore` builds `SigningCredentials` with fixed algorithm constant.

## Problem

Without declared default, callers may pick weak key sizes or mismatched algorithms, and the store and generator could drift apart, producing keys the store cannot use or verify consistently.

## Decision

The default signing key is RSA with 4096-bit modulus, and signatures use `SecurityAlgorithms.RsaSha256` (RS256). The default size is expressed once as the `rsaBits = 4096` parameter on `IKeyGenerator.Generate` and on `JwtKeyStore.RotateAsync`, while the algorithm is fixed at credential creation time; the size remains overridable by the caller for rotation.

### Design Rationale

- RSA-4096 gives conservative security margin for long lived signing keys.
- RS256 is widely supported by JWT verifiers and pairs naturally with the RSA keys and JWKS published by the store.
- Centralizing the default keeps generator and store aligned and makes future algorithm change single point edit.

## Rejected

- Defaulting to RSA-2048 or smaller for "performance".
- Making ECDSA the default algorithm.
- Hardcoding the algorithm inside the store while leaving the generator free to choose different one.

## Consequences

New keys are consistently strong and verifiable across the ecosystem. The cost is0 larger key size (more CPU per signature) and the need to revisit the default if the threat model or standard support changes.

## Related

- [ADR-001](./001-centralize-signing-key-management.md) - key store building credentials
- [ADR-006](./006-kid-as-generated-guid.md) - identifier generated alongside the key

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./006-kid-as-generated-guid.md) | [Next](./008-standardized-error-response.md)
