[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./024-plugin-lifecycle-and-hosted-services.md) | [Next]()

# [ADR-025] Keep Plugin Options, OpenAPI, And Marten Integrations Explicit

*2026-09* | Status: accepted

**Tag:** #adr_025

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host + optional plugin integrations

## Context

Plugins need strongly typed options, OpenAPI contributions, and optional Marten document configuration. These integrations have different dependency boundaries: options belong to the base contract, while Swashbuckle and Marten are infrastructure concerns.

## Decision

`IAuthKitPlugin.BindConfiguration<TOptions>` binds through the standard .NET options system from `Plugins:{Name}`. Plugins therefore receive normal `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` services without a custom registry.

OpenAPI and Marten integration contracts live in the optional `AuthKit.Plugins.Integrations` project:

- `IOpenApiPlugin.ConfigureOpenApi(SwaggerGenOptions)` receives the actual Swagger options owned by the Host;
- `IMartenPlugin.ConfigureMarten(StoreOptions)` receives the actual Marten options owned by the Host.

The Host invokes both hooks in stable plugin ID order. OpenAPI hooks run inside the existing `AddSwaggerGen` configuration. Marten hooks run inside the existing `AddMarten` configuration. Exceptions are allowed to fail host configuration; contributions are never silently discarded.

## Rationale

The base plugin contract stays independent of optional persistence and documentation infrastructure. Plugins that need either integration reference the optional integration project, while ordinary plugins retain the smaller abstraction dependency.

## Rejected

- Adding Swashbuckle or Marten references to the base abstractions project would force unrelated plugins to depend on optional host infrastructure.
- A custom options registry would duplicate the standard .NET options and DI mechanisms.
- Creating separate Swagger or Marten option instances would disconnect plugin contributions from the actual host configuration.

## Consequences

Plugin configuration is isolated and strongly typed. OpenAPI and Marten contributions are deterministic and share the host-owned configuration objects. Hosts without Marten do not need to reference the integration contract or invoke its hook.

## Related

- [ADR-022](./022-plugin-configuration-context-and-builder.md) - plugin configuration context
- [ADR-023](./023-plugin-application-pipeline-hooks.md) - application integration hooks
- [Issue #11](https://github.com/AuthKits/AuthKit.Server/issues/11) - options, OpenAPI, and Marten integration requirements

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./024-plugin-lifecycle-and-hosted-services.md) | [Next]()