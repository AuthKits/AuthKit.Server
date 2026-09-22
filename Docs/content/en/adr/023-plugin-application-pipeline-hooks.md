[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./022-plugin-configuration-context-and-builder.md) | [Next](./024-plugin-lifecycle-and-hosted-services.md)

# [ADR-023] Integrate Plugin Endpoints And Middleware Through Explicit Host Pipeline Hooks

*2026-09* | Status: accepted

**Tag:** #adr_023

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Context

Plugins need to contribute endpoints and request middleware, but ASP.NET Core middleware ordering is part of application behavior. Discovery order, filesystem order, or assembly load order must not decide where plugin code runs.

## Problem

The original contract exposed only `MiddlewareType`, which provided one implicit middleware slot and no endpoint registration hook. Plugins could not explicitly place middleware relative to routing, authentication, authorization, or endpoint execution.

## Decision

The contract adds optional default hooks:

- `MapEndpoints(IEndpointRouteBuilder)` for normal ASP.NET Core endpoint routing;
- `ConfigureApplication(IApplicationBuilder)` for plugin application configuration;
- `ConfigurePipeline(IApplicationBuilder, PluginPipelinePosition)` for explicitly positioned middleware;
- `PipelinePosition`, using the strongly typed `PluginPipelinePosition` enum.

The supported positions are `BeforeRouting`, `AfterRouting`, `BeforeAuthentication`, `AfterAuthentication`, `BeforeAuthorization`, `AfterAuthorization`, `BeforeEndpoints`, and `AfterEndpoints`.

The Host applies hooks to the real application builder and endpoint route builder. Plugins at the same position are ordered by stable `Plugin.Id`, independently of discovery order. `AfterEndpoints` runs after REST and gRPC endpoint mapping.

Existing `MiddlewareType` behavior remains available in its original slot. If a plugin implements `ConfigureApplication` or `ConfigurePipeline`, the Host does not also register its `MiddlewareType`, preventing accidental duplicate middleware registration. Hook exceptions and invalid positions are surfaced explicitly.

### Design Rationale

- Endpoint hooks use the normal ASP.NET Core routing system, preserving endpoint metadata, authorization, authentication, OpenAPI discovery, and endpoint selection.
- A finite enum exposes meaningful pipeline stages without making every internal middleware implementation a public dependency.
- Sorting by stable plugin ID makes equal-position ordering deterministic and testable.
- Default interface members keep plugins that only use `MiddlewareType` source-compatible.

## Rejected

- Keeping middleware order equal to plugin discovery order is nondeterministic.
- Arbitrary string positions are weakly typed and cannot be validated reliably.
- A parallel endpoint router would bypass ASP.NET Core endpoint metadata and selection.
- Silently moving invalid positions to the end would hide plugin configuration errors.

## Consequences

Plugins can participate in the host's endpoint and middleware pipeline without modifying host startup code. Pipeline placement is explicit and reviewable. Plugin authors must choose a supported stage when using `ConfigurePipeline`; the Host owns the stage boundaries and deterministic ordering.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery and contract boundary
- [ADR-010](./010-plugin-loading-from-directory.md) - plugin loading and legacy middleware slot
- [Issue #9](https://github.com/AuthKits/AuthKit.Server/issues/9) - endpoint and application pipeline requirements

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./022-plugin-configuration-context-and-builder.md) | [Next](./024-plugin-lifecycle-and-hosted-services.md)
