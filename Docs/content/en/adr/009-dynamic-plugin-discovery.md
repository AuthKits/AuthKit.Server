[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./008-standardized-error-response.md) | [Next](./010-plugin-loading-from-directory.md)

# [ADR-009] Discover And Load Plugins Dynamically Through The IAuthKitPlugin Contract

*2026-08* | Status: accepted

**Tag:** #adr_009

**Date:** 2026-08-26

**Scope:** AuthKit.Plugins.Abstractions

## Context

AuthKit.Server is composed of host plus optional solution packages (for example, the `DevTokens` developer token solution). The host must be able to grow its feature set new services, middleware, health checks, and authentication schemes - without recompiling or directly referencing every solution at build time.

## Problem

If solutions are wired into the host through static project references and hand written registration, the host couples to each feature and every new capability becomes host change. The host also needs uniform way to let an extension contribute to the DI container, request pipeline, health surface, and OpenAPI metadata, while keeping each extension isolated behind stable abstractions.

## Decision

Plugins implement the `IAuthKitPlugin` contract and are discovered and loaded dynamically by the host at startup. plugin does **not** need to be directly referenced by the host project. An extension contributes to the running server through well defined, optional members of the contract.

### IAuthKitPlugin

**Responsibilities:**

- Identify the plugin (`Name`, `Version`, optional `Description`) for diagnostics and metadata.
- Register the plugin's services in the host DI container via `ConfigureServices` (the plugin must not create its own container).
- Optionally contribute an ASP.NET Core middleware type (`MiddlewareType`) inserted by the host at the plugin pipeline slot.
- Optionally expose health signal via `CheckHealthAsync`, resolved from the host's root service provider.
- Optionally contribute transport agnostic OpenAPI security schemes via `GetSecuritySchemes`.

### AuthKitSecuritySchemeDescriptor

**Responsibilities:**

- Describe an authentication mechanism (API key, HTTP, OAuth2, OpenID Connect) at the metadata level.
- Remain transport agnostic so the same scheme can back HTTP headers and gRPC metadata.

### Design Rationale

- Dynamic discovery keeps the host decoupled from concrete solutions and lets features ship as drop-in packages.
- A single contract with sensible default implementations for optional members keeps plugins small and the host's integration code uniform.
- Isolation behind `AuthKit.Plugins.Abstractions` means plugins depend only on the contract, not on host internals signing keys and token to key bindings stay owned by the core and are consumed through DI rather than reimplemented.

## Rejected

- Static project references and compile time registration for each solution.
- Letting each plugin create and manage its own dependency injection container.
- Hardcoding plugin middleware or health checks in the host instead of discovering them from the contract.
- Embedding transport-specific authentication details directly in plugin code rather than through transport agnostic descriptor.

## Consequences

The host stays stable while features are added as independently loadable plugins, and each plugin integrates through one predictable surface. The cost is discovery/loading mechanism at startup and the discipline of keeping plugin behavior within the contract's boundaries plugins must rely on injected core services (such as signing keys) rather than reaching into host internals.

## Related

- [ADR-010](./010-plugin-loading-from-directory.md) - host-side plugin discovery and loading
- [ADR-004](./004-token-key-bindings-domain.md) - plugins consume core bindings through the contract
- [ADR-016](./016-marten-and-wolverine-infrastructure.md) - plugin handlers run on this infrastructure

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./008-standardized-error-response.md) | [Next](./010-plugin-loading-from-directory.md)
