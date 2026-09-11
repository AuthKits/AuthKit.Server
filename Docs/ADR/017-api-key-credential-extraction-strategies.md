[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./016-marten-and-wolverine-infrastructure.md) | [Next](./018-security-scheme-contract-explicit-handling.md)

# [ADR-017] Extract API Key Credentials Through Location And Body Format Strategies

*2026-09* | Status: accepted

**Tag:** #adr_017

**Date:** 2026-09-10

**Scope:** Host.Security

## Context

Plugins expose authentication mechanisms as transport-agnostic `AuthKitSecuritySchemeDescriptor` metadata (ADR-009). For API key schemes the descriptor declares where the credential lives through `AuthKitApiKeyLocation` (header, query, cookie, gRPC metadata, or request body). The host must turn that metadata into retrieval of an actual credential from an incoming asp.net request, validate it, and establish an authenticated identity without coupling scheme authors to http mechanics.

## Problem

A single extractor/middleware that internally handles all five locations plus body buffering, JSON parsing, form parsing, and value normalization becomes an orchestrator and implementation god object. Adding a new location or a new body format requires modifying existing code, and the low-level transport mechanics (Buffering, JSON, form decoding) are entangled with the orchestration flow (extract -> validate -> identity).

## Decision

API key credential retrieval is split into small, single responsibility strategies registered in the host DI container under `Host.Security`:

- **Location strategies** implement `IApiKeyLocationExtractor`, one per `AuthKitApiKeyLocation` (`Header`, `Query`, `Cookie`, `GrpcMetadata`, `Body`). Each strategy knows only how to read its own location and normalize the value.
- **`IApiKeyLocationExtractorRegistry`** (DI backed) maps `AuthKitApiKeyLocation` -> strategy. The registry throws `NotSupportedException` for locations without registered strategy, so unsupported configurations fail fast.
- **Body format parsers** implement `IApiKeyBodyParser` and are selected by request content type. Today `JsonApiKeyBodyParser` and `FormUrlEncodedApiKeyBodyParser` are registered; a new format is a new class plus registration.
- **`ApiKeyCredentialExtractor`** is a thin middleware that only orchestrates: resolve the scheme, extract via the registry, validate via `IApiKeyValidator`, and on success attach the principal's claims as an authenticated identity. It contains no parsing or buffering logic.
- **`IApiKeyValidator`** stays the plugin facing contract for validating an extracted key, keeping credential semantics out of the host.
- **`ApiKeyCredentialExtractorOptions`** centralizes defaults (header/query/cookie field names, body buffer threshold) and is consumed through `IOptions`.
- All of it is registered by `AddApiKeyCredentialExtraction(...)`.

Body extraction buffers the request through `EnableBuffering`, reads the payload, and restores the stream position so downstream middleware and controllers still see the body intact.

### Design Rationale

- **Open/Closed**: a new location or body format adds class and a DI registration without editing any existing strategy, parser, or the middleware.
- **Single Responsibility**: the middleware owns orchestration; each strategy/parser owns exactly one mechanical concern.
- **Dependency Inversion**: the middleware depends on the registry and validator abstractions, not on `HttpContext` parsing or JSON.
- **Fail open orchestration**: when no credential or no principal is produced, the pipeline continues so downstream authentication decides the outcome extraction never aborts an unrelated request.

## Rejected

- A single switch/if extractor with inline body and JSON logic every new location or format modifies the orchestrator.
- Basing parsing decisions on the scheme inside the middleware couples orchestration to format mechanics.
- Requiring each plugin to implement its own extraction duplicates `HttpContext` parsing, buffering, and normalization across solutions.
- Silent conversion of unsupported locations (eg. treating `GrpcMetadata` as plain HTTP headers without registered strategy) hides misconfiguration.

## Consequences

`Host.Security` now contains many small types instead of one large one adding location or format costs files and registration, but never an edit to existing behavior. The registry makes genuinely unsupported locations fail fast at resolution. Body parsing is content type selective, so ambiguous payloads yield no credential (fail open) rather than an error validation stays plugin concern behind `IApiKeyValidator`, and the host only attaches claims after successful validation.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - descriptors and pluggable authentication contributed by plugins
- [ADR-010](./010-plugin-loading-from-directory.md) - plugin middleware slot where extraction runs
- [ADR-013](./013-dual-rest-and-grpc-transport.md) - gRPC metadata transport represented as lowercase HTTP headers

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./016-marten-and-wolverine-infrastructure.md) | [Next](./018-security-scheme-contract-explicit-handling.md)