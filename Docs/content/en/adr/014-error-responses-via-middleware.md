[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./013-dual-rest-and-grpc-transport.md) | [Next](./015-keycloak-external-jwt-authority.md)

# [ADR-014] Render HTTP Errors As RFC 7807 Problem Details Via Middleware

*2026-08* | Status: accepted

**Tag:** #adr_014

**Date:** 2026-08-26

**Scope:** Host.Restful.Middleware.Exceptions

## Context

ADR-008 establishes that the Core owns shared error vocabulary (`DomainException`, `ErrorResponse`, `ErrorMetadataOptions`). The host must turn failures - domain violations, validation failures, and authorization outcomes - into consistent HTTP shape without leaking internals.

## Problem

Scattering error handling inside every controller produces inconsistent responses and risks exposing stack traces. The wire format must be standard and machine readable, and it must stay tied to the Core error vocabulary.

## Decision

Three host components centralize HTTP error rendering, all emitting RFC 7807 `ProblemDetails` with `error_code`, `trace_id`, and documentation `type` built from `ErrorMetadataOptions.DocsBaseUrl`:

- `ExceptionHandlingMiddleware` maps `DomainException` to `409 Conflict` (code derived from the exception type) and any other exception to `500` without internal details.
- `ValidationExceptionMiddleware` maps FluentValidation `ValidationException` to `400 Bad Request` with errors grouped by property.
- `CustomAuthorizationMiddlewareResultHandler` maps challenge/forbidden to `401`/`403`.

### Design Rationale

- A middleware pipeline catches errors at one, so controllers stay clean and responses are uniform.
- RFC 7807 is standard problem format understood by HTTP clients and code generators.
- Mapping from `DomainException` keeps the Core error vocabulary (ADR-008) as the single source of domain error meaning, while the host chooses the concrete wire representation.

## Rejected

- Returning the Core `ErrorResponse` DTO shape directly as the only HTTP contract (less tooling friendly than ProblemDetails).
- Letting exceptions propagate to the framework default page.
- Exposing exception messages or stack traces for non domain errors.

## Consequences

HTTP clients get predictable, standards based error body with codes and trace ids, and domain errors remain authored in Core. The cost is maintaining two related but distinct shapes - the Core `ErrorResponse` vocabulary and the host `ProblemDetails` rendering keeping the mapping in sync as new domain exceptions appear.

## Related

- [ADR-008](./008-standardized-error-response.md) - Core error vocabulary rendered here
- [ADR-013](./013-dual-rest-and-grpc-transport.md) - applies to the REST surface

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./013-dual-rest-and-grpc-transport.md) | [Next](./015-keycloak-external-jwt-authority.md)
