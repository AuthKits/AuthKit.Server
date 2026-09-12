[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./025-plugin-options-openapi-and-marten-integrations.md) | [Next]()

# [ADR-026] Configure Plugin Authentication And Authorization Through Host Security Infrastructure

*2026-09* | Status: accepted

**Tag:** #adr_026

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host security configuration

## Context

Plugins may need to add authentication schemes and authorization policies. Those registrations must participate in the same ASP.NET Core security infrastructure as the host and must remain compatible with existing JWT, middleware, endpoint routing, and security-scheme metadata.

## Decision

`IAuthKitPlugin` exposes optional default hooks:

- `ConfigureAuthentication(AuthenticationBuilder)`;
- `ConfigureAuthorization(AuthorizationOptions)`.

The Host invokes the hooks in stable plugin ID order. It passes the real host-owned builders/options after registering the existing Keycloak JWT default and before the application is built. Plugin schemes do not become the default authentication scheme automatically. Plugin policies are added to the same `AuthorizationOptions` used by ASP.NET Core.

Hook exceptions are propagated. Scheme and policy names are globally significant; plugins should use namespaced names such as `plugin.read`. The Host does not silently rename, replace, or map security configuration. New authentication transport and OpenAPI security descriptor behavior remains governed by the existing security-scheme contract.

## Rationale

Using the actual ASP.NET Core builders preserves standard scheme selection, policy providers, authentication challenges, endpoint authorization metadata, and middleware behavior. Default interface implementations keep existing plugins source-compatible.

## Rejected

- A separate plugin authentication service provider would disconnect schemes from the host.
- Automatically making plugin schemes default could change existing host behavior.
- A parallel policy registry would bypass `IAuthorizationPolicyProvider`.
- Rewriting security scheme descriptors into authentication registrations would conflate documentation metadata with runtime security configuration.

## Consequences

Plugin schemes and policies can protect plugin endpoints through normal ASP.NET Core APIs. Colliding global names remain configuration errors governed by the host/framework configuration path and should be avoided through plugin namespacing. Existing JWT authentication and authorization behavior remains the default unless a plugin explicitly contributes additional configuration.

## Related

- [ADR-018](./018-security-scheme-contract-explicit-handling.md) - security scheme metadata and OpenAPI mapping
- [ADR-023](./023-plugin-application-pipeline-hooks.md) - plugin endpoints and middleware pipeline
- [Issue #12](https://github.com/AuthKits/AuthKit.Server/issues/12) - authentication and authorization hooks

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./025-plugin-options-openapi-and-marten-integrations.md) | [Next]()