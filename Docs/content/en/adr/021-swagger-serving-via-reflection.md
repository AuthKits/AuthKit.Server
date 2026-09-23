[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./020-devtools-plugin.md) | [Next]()

# [ADR-021] Serve Swagger Through Reflection-Based SwaggerHost In DevTools

*2026-09* | Status: accepted

**Tag:** #adr_021

**Date:** 2026-09-11

**Scope:** Plugins.Solutions.DevTools

## Context

ADR-020 moved Swagger UI serving from the host into the `DevTools` plugin. The plugin already consumed the OpenAPI document Provider (`ISwaggerProvider`, registered by `RestfulConfiguration` + `AddSwaggerGen`), but the actual document serialization and HTML UI rendering were performed by Swashbuckle's `SwaggerMiddleware` and `SwaggerUIMiddleware` — both of which are **internal** to their NuGet packages, so a plugin cannot call them the way the host used to.

## Problem

The plugin must reproduce what the host previously did with the public `UseSwagger` / `UseSwaggerUI` extension methods, but the middlewares behind those extensions are not part of the public API surface. The plugin also needs to control the OpenAPI spec version pinned for the served document — independently of any document that `RestfulConfiguration` might generate for other consumers.

## Decision

`DevTools` serves Swagger with a reflection-based `SwaggerHost`:

- `SwaggerHost` resolves the internal `Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware` and `Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware` types by assembly-qualified name at startup, constructs one instance per middleware via `Activator.CreateInstance` with the standard (notFound, options) constructor shape, and invokes the internal `Invoke` methods reflectively per request.
- The Swagger document middleware is configured with a route template under the configured Swagger route prefix and the resolved OpenAPI spec version.
- The Swagger UI middleware receives UI options (route prefix, document title, `IndexStream` pointing at the embedded `index.html` resource) and an endpoint pointing at the document route.
- `DevToolsMiddleware` decides whether a request path belongs to the plugin (gRPC UI, Swagger, landing page) and, for the Swagger segment, delegates to `SwaggerHost.TryServeAsync`; a 404-not-found `RequestDelegate` is handed to the Swashbuckle middlewares when the path does not match.
- The OpenAPI spec version for the served document is resolved by `SwaggerHost.ResolveSpecVersion`: a `DevTools:Swagger:SpecVersion` override, then the host's `OpenApi:SpecVersion`, defaulting to `3.0`. Supported values are `3.0.x` and `3.1.x`; an unknown value logs a warning and falls back to `3.0`.
- Because the served document uses the spec version pinned for DevTools, mutual-TLS schemes (ADR-018) surface natively (`mutualTLS`) only when the document is pinned to OpenAPI 3.1.

### Design Rationale

- **Reuse over reimplementation**: the plugin does not re-implement OpenAPI serialization or the HTML UI — it drives the exact middleware the host used, through reflection, because the types are internal.
- **Single construction**: middleware instances are built once at plugin startup and reused for every request; per-request work is a reflection invoke, not a rebuild.
- **Independent pinning**: DevTools controls its own document spec version rather than inheriting a fixed host document, which is what lets the OpenAPI mapper's 3.1-dependent cases (ADR-018) work in DevTools.

## Rejected

- Re-implementing the Swagger UI page and OpenAPI serialization inside the plugin — duplicates a large, maintained surface for no architectural gain.
- Depending on the internal Swashbuckle types as a hard library reference — they are internal by design; a hard reference simply would not compile.
- Copying/shipping the Swashbuckle middleware source into the plugin — license and maintenance burden, and drift from upstream bug fixes.
- Removing Swagger serving from DevTools entirely (document-only JSON at a well-known URL) — loses the interactive UI that motivated the plugin.

## Consequences

`DevTools` stays on the Swashbuckle version that ships the internal middlewares; upgrading a Swashbuckle version could change the internal type/methods signatures and break `SwaggerHost` at startup (fail-fast with a descriptive exception). The reflection layer is a deliberate, visible cost: two `Type`/`MethodInfo` lookups and a per-request `Invoke` instead of direct calls. Unknown spec versions degrade to a warning + 3.0 in DevTools serving (so the UI still works), which is intentionally lazier than `RestfulConfiguration`, which hard-fails on unknown spec versions in app configuration.

## Related

- [ADR-013](./013-dual-rest-and-grpc-transport.md) - the OpenAPI surface served by DevTools
- [ADR-020](./020-devtools-plugin.md) - why Swagger serving lives in the plugin at all
- [ADR-018](./018-security-scheme-contract-explicit-handling.md) - mutual-TLS schemes require the 3.1 pin DevTools provides

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./020-devtools-plugin.md) | [Next]()