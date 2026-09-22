[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./011-keystore-persisted-as-singleton-marten-document.md) | [Next](./013-dual-rest-and-grpc-transport.md)

# [ADR-012] Persist Token Key Bindings In Marten

*2026-08* | Status: accepted

**Tag:** #adr_012

**Date:** 2026-08-26

**Scope:** Host.TokenKeyBindings.Repositories

## Context

ADR-004 makes token to signing key bindings core domain, and ADR-011 persists the encrypted signing key material itself as singleton Marten document. The host must decide where binding state lives so it survives restarts alongside the keys it references.

## Problem

Bindings link developer tokens to signing keys and support rotation, public key updates, and revocation. If they are kept only in memory they are lost on restart, forcing re-establishment of every token -> key association and breaking verification of already issued tokens after redeploy. The repository must still stay free of domain logic and use the same store the rest of the host already depends on.

## Decision

`KeyBindingRepository` persists each `TokenKeyBinding` as Marten document identified by composite id built from the developer token id and signing key id (`"{tokenId:N}:{signingKeyId}"`). It uses lightweight Marten sessions, stores only the serialized binding, and performs no transitions or cryptography.

### KeyBindingRepository

**Responsibilities:**

- Add/get/update/list `TokenKeyBinding` entities via Marten documents.
- Address each binding by its `(tokenId, signingKeyId)` composite identity.
- Stay pure persistence boundary, like `KeyStoreRepository` (ADR-011).

### KeyBindingDocument

**Responsibilities:**

- Carry the Marten document id and the persisted `TokenKeyBinding` payload.

### Design Rationale

- Durable persistence keeps token -> key provenance and revocation state across restarts, so issued tokens stay verifiable after redeploy.
- Reusing Marten (ADR-016) avoids second datastore and keeps bindings consistent with the keystore transactionally and operationally.
- A composite document id matches the binding's natural identity and makes `GetAsync` and `ListByTokenAsync` direct lookups/queries.

## Rejected

- A separate flat file or custom blob store outside Marten.
- Letting the repository perform binding transitions or encryption.

## Consequences

Token key bindings are durable and queryable like the rest of the host's data, and the design stays clean `IKeyBindingRepository` implementation. The cost is hard dependency on Marten/Postgres for binding availability (the same dependency already required by the keystore) and the discipline that the repository never inspects or mutates the binding payload.

## Related

- [ADR-004](./004-token-key-bindings-domain.md) - the domain persisted here
- [ADR-011](./011-keystore-persisted-as-singleton-marten-document.md) - keystore persisted the same way
- [ADR-016](./016-marten-and-wolverine-infrastructure.md) - Marten as the host store

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./011-keystore-persisted-as-singleton-marten-document.md) | [Next](./013-dual-rest-and-grpc-transport.md)
