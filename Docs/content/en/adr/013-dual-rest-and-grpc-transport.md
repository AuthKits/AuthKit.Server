[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./012-token-key-bindings-persisted-in-marten.md) | [Next](./014-error-responses-via-middleware.md)

# [ADR-013] Expose Both REST And gRPC Transport Surfaces

*2026-08* | Status: accepted

**Tag:** #adr_013

**Date:** 2026-08-26

**Scope:** Host

## Context

The host must serve API clients with different needs conventional HTTP/JSON consumers and high performance/contract first gRPC clients. Both surfaces sit on top of the same Core domain and plugin contributions.

## Problem

Committing to single transport either excludes class of clients or forces an awkward adaptation layer. Running two separate processes duplicates composition, configuration, and operational surface.

## Decision

The host is single ASP.NET Core application that exposes both RESTful surface (controllers/MVC via `AddRestfulServices` and `MapAppEndpoints`) and gRPC surface (`AddGrpcServices` and `MapGrpcEndpoints`) from one composed pipeline. Both are built on the same Core services, the same plugin contributions (ADR-009/010), and the same infrastructure (ADR-016).

### Design Rationale

- One process and one composition root keep configuration, DI, middleware, and plugin loading shared across transports.
- REST covers broad HTTP/JSON interoperability; gRPC covers typed, low overhead contracts from the same domain.
- A single pipeline avoids drift between what each transport can do.

## Rejected

- Exposing only REST or only gRPC.
- Splitting the surfaces into separate deployable processes.
- Tunneling one transport over the other instead of native endpoints.

## Consequences

Clients can choose the transport that fits them without second deployment, and both stay consistent with Core. The cost is maintaining two contract sets (OpenAPI and protobuf) and ensuring both surfaces reflect the same domain behavior and plugin-provided security schemes.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - both surfaces expose plugin contributions
- [ADR-014](./014-error-responses-via-middleware.md) - unified error rendering for both transports
- [ADR-015](./015-keycloak-external-jwt-authority.md) - auth protecting both surfaces

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./012-token-key-bindings-persisted-in-marten.md) | [Next](./014-error-responses-via-middleware.md)
