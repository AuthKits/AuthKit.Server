[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./019-plugin-metadata-attribute.md) | [Next](./021-swagger-serving-via-reflection.md)

# [ADR-020] Host Developer Tools Through A Dedicated DevTools Plugin

*2026-09* | Status: accepted

**Tag:** #adr_020

**Date:** 2026-09-11

**Scope:** Host + Plugins.Solutions.DevTools

## Context

AuthKit needs developer facing tooling to exercise its own surfaces: an interactive page to call any exposed gRPC method, and Swagger UI to browse the REST/OpenAPI surface. Earlier work exposed a gRPC UI as a standalone `GrpcUI` plugin and served Swagger UI on the host itself through `UseSwagger` / `UseSwaggerUI` (Swashbuckle direct middleware). The two tools lived in different architectural homes: one was a plugin, the other was hard wired into the host's request pipeline.

## Problem

A tool baked into the host can only ever be enabled together with the host, couples the host to Swashbuckle's serving middleware, and cannot be shared across host flavors or removed without editing host code. A dedicated `GrpcUI` plugin covered gRPC but left the REST/OpenAPI tooling outside the plugin boundary two mechanisms for the same job, two places to configure. Tooling URLs were also fixed implicit host paths rather than configurable plugin owned prefixes.

## Decision

Both developer tools move into single `DevTools` plugin (`src/Plugins/Solutions/DevTools`), replacing the standalone `GrpcUI` plugin and removing Swagger serving from the host:

- The plugin owns its URL space through `DevToolsOptions`: gRPC UI at `GrpcUiPathBase` (`/grpc-ui`), Swagger UI under the `Swagger` section (default route prefix `swagger`, document name `v1`, title `AuthKit API`), and a landing page at `PathBase` (`/devtools`).
- The gRPC UI is in process: `GrpcServiceCatalog` scans the default assembly load context for static `ServiceDescriptor` properties, `GrpcDynamicInvoker` executes unary methods dynamically over `Grpc.Net.Client` without generated stubs, and `DevToolsMiddleware` dispatches `/grpc-ui` and its `/api/services` + `/api/invoke` endpoints.
- Swagger document generation stays in the host (`RestfulConfiguration` + `AddSwaggerGen`); only the SWAGGER SERVING middleware moves into `DevTools` via `SwaggerHost`.
- The host no longer calls `UseSwagger` / `UseSwaggerUI` `AppMiddlewareConfiguration` contains only the plugin slot. Developer tools are present in a deployment exactly when the plugin is deployed.
- Swagger UI serving is gated: by default enabled only in the development environment (`Swagger.Enabled ?? environment.IsDevelopment()`), with pinning of the OpenAPI spec version for the served document.
- The host is published with `DevTools` selected by default (solution file and Dockerfile), so a default deployment keeps both tools available.

### Design Rationale

- **One tooling surface**: REST (Swagger) and gRPC (in-process UI) are both interactive representations of the same host one plugin owns the developer experience.
- **Consistent plugin boundary**: everything a template developer visits is contributed by plugin, under plugin owned path bases, removable by not deploying the plugin.
- **In-process over proxy**: the gRPC UI talks to the host directly rather than fronting separate process and using gRPC reflection, keeping discovery local and configuration minimal.
- **No stubs needed**: dynamic invocation over method descriptors means the plugin never needs generated client code for the services it renders.

## Rejected

- Keeping Swagger serving in the host (`UseSwagger`/`UseSwaggerUI`) couples host to specific tool and a specific serving middleware.
- A standalone `GrpcUI` plugin with host owned Swagger two homes for equivalent functionality.
- A gRPC proxy process (eg. maintained third party gRPC UI binary) for streaming and health support additional deployment process and transport hop the extra support was deemed unnecessary for an internal developer tool.
- Serving Swagger from the plugin while re-emitting document generation there would duplicate `AddSwaggerGen` configuration generation stays in the host, serving stays in the plugin.

## HTTP API Contract

The DevTools plugin exposes two JSON API endpoints consumed by the TypeScript frontend:

### `/api/services` — GrpcCatalogResponse

Returns the discovered gRPC services for the catalog:

```csharp
public sealed class GrpcCatalogResponse
{
    public required string Target { get; init; }
    public required IEnumerable<GrpcServiceInfo> Services { get; init; }
}

public sealed class GrpcServiceInfo
{
    public required string Name { get; init; }      // Short service name
    public required string FullName { get; init; }  // Fully qualified name
    public string? Description { get; init; }       // Nullable proto doc comment (null = absent)
    public required string Package { get; init; }
    public required string FileName { get; init; }
    public bool IsPlugin { get; init; }              // Default: false (backward-compatible for host services)
    public required IEnumerable<GrpcMethodInfo> Methods { get; init; }
}
```

- `Description`: Nullable string from proto documentation. Absent (`null`) when proto has no comment.
- `IsPlugin`: Defaults to `false` for host services, enabling backward compatibility with non-plugin assemblies.

### `/api/invoke` — GrpcInvocationRequest → GrpcInvocationResult

Executes a unary gRPC method:

```csharp
public sealed class GrpcInvocationRequest
{
    public required string Service { get; init; }
    public required string Method { get; init; }
    public required string RequestJson { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}

public sealed class GrpcInvocationResult
{
    public required bool Success { get; init; }
    public required string StatusName { get; init; }
    public required int StatusCode { get; init; }
    public string? Detail { get; init; }
    public string? ResponseJson { get; init; }
    public string? ResponseBase64 { get; init; }    // Success-only base64-encoded protobuf payload; null on failure
    public double ElapsedMs { get; init; }
    public IReadOnlyDictionary<string, string>? Trailers { get; init; }
}
```

- `ResponseBase64`: Only populated on successful invocations (`Success == true`). Contains raw protobuf bytes encoded as base64.
- **Payload size limit**: The base64 payload is limited to `int.MaxValue - 1` bytes (≈2 GB) due to protobuf size constraints.
- `ResponseJson`: Present only on success, containing JSON-formatted response via protobuf JSON formatter.
- Failed invocations return `Success = false` with `Detail` and `Trailers` populated for debugging.

### JSON Serialization

All DTOs are serialized using `System.Text.Json` with `JsonSerializerDefaults.Web` for consistent casing:

- PascalCase property names in JSON (e.g., `"ResponseBase64"`, `"IsPlugin"`).
- Nullable fields are omitted when null (default ASP.NET Core JSON behavior).
- Boolean fields with `false` values are included (not omitted).

## Consequences

The host's `AppMiddlewareConfiguration` is smaller and no longer references Swashbuckle. The `DevTools` solution owns compiler friendly tooling code (catalog, invoker, middleware, options, and UI assets). Swagger UI availability depends on plugin deployment and environment gating rather than host build configuration. The gRPC UI only supports unary methods streaming methods are reported as unsupported rather than approximated. Configuration is centralized in `DevToolsOptions`, resolved with environment variables (`GRPC_UI_TARGET`, `DEV_CERT_PORT_GRPC`) for local development against the HTTPS gRPC endpoint.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin contract and dynamic discovery
- [ADR-010](./010-plugin-loading-from-directory.md) - plugin middleware slot used by DevToolsMiddleware
- [ADR-013](./013-dual-rest-and-grpc-transport.md) - dual REST + gRPC surfaces being exercised
- [ADR-021](./021-swagger-serving-via-reflection.md) - how SwaggerHost serves SwaggerUI through reflection

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./019-plugin-metadata-attribute.md) | [Next](./021-swagger-serving-via-reflection.md)