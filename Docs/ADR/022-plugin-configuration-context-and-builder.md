[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./021-swagger-serving-via-reflection.md) | [Next](./023-plugin-application-pipeline-hooks.md)

# [ADR-022] Extend Plugin Configuration With The Host Builder And Scoped Context

*2026-09* | Status: accepted

**Tag:** #adr_022

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Context

Plugins previously configured services through `ConfigureServices(IServiceCollection, IConfiguration)`. That was sufficient for registrations, but it did not expose the actual host builder or a stable plugin-specific configuration context.

## Problem

Adding abstract members to `IAuthKitPlugin` would break existing plugins. Passing more individual host dependencies would also make the contract difficult to evolve and would encourage plugins to depend on host internals.

## Decision

The plugin contract exposes additive default interface members:

- `ConfigureServices(IHostApplicationBuilder, IConfiguration)` for plugins that need the real AuthKit host builder.
- `ConfigureServices(IServiceCollection, AuthKitPluginContext)` for plugins that need stable plugin identity and configuration context.
- The existing `ConfigureServices(IServiceCollection, IConfiguration)` remains valid for legacy plugins.

`AuthKitPluginContext` lives in the root `AuthKit.Plugins.Abstractions` namespace and exposes:

- stable `PluginId`;
- `PluginName`;
- plugin-scoped `Configuration` from `Plugins:{PluginId}` with a name fallback;
- read-only full `ApplicationConfiguration` for host-level settings.

The Host uses one dispatcher. It selects context configuration first, then host-builder configuration, then the legacy overload. Only one overload is invoked for a plugin, so compatibility paths cannot register the same services twice.

### Design Rationale

- Default interface implementations preserve source compatibility.
- The actual `WebApplicationBuilder` is passed instead of constructing an isolated builder.
- Plugin-scoped configuration prevents one plugin from accidentally reading another plugin's settings.
- The full application configuration remains available explicitly without creating a second DI or configuration system.

## Rejected

- Making the new overloads abstract would break existing plugins.
- Constructing a separate host builder would disconnect registrations from the running application.
- Passing `IServiceProvider` through the context would introduce service-locator behavior.
- Invoking every overload would cause duplicate registration and ambiguous behavior.

## Consequences

New plugins can opt into host-aware configuration or scoped context data. Existing plugins such as DevTokens and DevTools continue to use their legacy implementation without source changes. The dispatcher is a Host concern and the public contract remains independent of the Host's internal plugin loader.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - the plugin contract and dynamic discovery
- [ADR-019](./019-plugin-metadata-attribute.md) - declarative plugin identity used by the context
- [Issue #8](https://github.com/AuthKits/AuthKit.Server/issues/8) - host builder and plugin context requirements

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./021-swagger-serving-via-reflection.md) | [Next](./023-plugin-application-pipeline-hooks.md)
