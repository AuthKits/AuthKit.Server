[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./014-error-responses-via-middleware.md) | [Next](./016-marten-and-wolverine-infrastructure.md)

# [ADR-015] Use Keycloak As The External JWT Authority

*2026-08* | Status: accepted

**Tag:** #adr_015

**Date:** 2026-08-26

**Scope:** Host.Configuration

## Context

The host needs to authenticate callers (including administrative and plugin protected endpoints) using signed JWTs issued by real identity provider, rather than minting or validating tokens itself.

## Problem

Rolling local user store or self issued tokens couples the host to identity logic it should not own, and clients expect standard OIDC/JWT bearer flows. The host must validate tokens and surface Keycloak roles to ASP.NET Core authorization without bespoke claim plumbing.

## Decision

`AddKeycloakServices` registers ASP.NET Core JWT bearer authentication against Keycloak realm: `Authority` and `Audience` come from `KEYCLOAK_URL`/`KEYCLOAK_REALM`/`KEYCLOAK_CLIENT_ID` (with local dev defaults), issuer and audience validation are on, `NameClaimType` is `preferred_username`, and an `OnTokenValidated` hook maps the Keycloak `resource_access` client roles into standard `ClaimTypes.Role` claims.

### Design Rationale

- Delegating to Keycloak keeps the host out of identity issuance and proves tokens via standard JWT bearer validation.
- Mapping `resource_access` roles to role claims lets ordinary ASP.NET Core policies authorize without Keycloak specific code in handlers.
- Environment driven configuration supports dev, test, and production realms from the same code.

## Rejected

- A self issued or local token service inside the host.
- A custom user/role database.
- Disabling issuer/audience validation.

## Consequences

Callers authenticate with standard Keycloak issued JWTs and role-based policies work uniformly. Two deliberate dev concessions are documented as tradeoffs: `RequireHttpsMetadata = false` and a `DangerousAcceptAnyServerCertificateValidator` backchannel handler - both must be tightened before any production deployment.

## Related

- [ADR-013](./013-dual-rest-and-grpc-transport.md) - protects both transport surfaces
- [ADR-014](./014-error-responses-via-middleware.md) - unauthorized/forbidden rendered here

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./014-error-responses-via-middleware.md) | [Next](./016-marten-and-wolverine-infrastructure.md)
