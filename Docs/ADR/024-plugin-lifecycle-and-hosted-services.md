[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./023-plugin-application-pipeline-hooks.md) | [Next]()

# [ADR-024] Bridge Plugin Lifecycle Hooks To The Standard .NET Host Lifecycle

*2026-09* | Status: accepted

**Tag:** #adr_024

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Context

Plugins may need to initialize runtime resources, perform work after startup, release external registrations during shutdown, or contribute background services. Static service registration cannot represent those operations safely.

## Problem

Without explicit lifecycle hooks, plugins would need host-specific startup code or custom hosted-service schedulers. Manually invoking plugin background services would also bypass the standard .NET host lifecycle and its cancellation semantics.

## Decision

`IAuthKitPlugin` exposes additive default members:

- `OnStartingAsync(CancellationToken)`;
- `OnStartedAsync(CancellationToken)`;
- `OnStoppingAsync(CancellationToken)`;
- `GetHostedServices()` returning a non-null `IReadOnlyList<IHostedService>`.

The Host uses one `PluginLifecycleHostedService` bridge registered through the normal DI container. It orders plugins by stable `Plugin.Id`:

- `OnStartingAsync` runs in ascending order during hosted-service startup;
- `OnStartedAsync` runs after `ApplicationStarted` and only after successful startup;
- `OnStoppingAsync` runs in reverse order when `ApplicationStopping` is signaled.

Plugin-provided hosted services are registered as singleton `IHostedService` instances before host startup. Their `StartAsync` and `StopAsync` methods are therefore invoked by the standard .NET host rather than by AuthKit code. Null results, null service instances, duplicate registration, and lifecycle exceptions are rejected explicitly. Lifecycle failures include the plugin ID and lifecycle stage in the thrown exception.

### Design Rationale

- Standard `IHostedService` integration preserves the framework's startup, shutdown, cancellation, and disposal behavior.
- One lifecycle bridge prevents duplicate hook invocation and avoids a custom scheduler.
- Stable ID ordering makes startup and shutdown deterministic regardless of discovery order.
- Default interface members preserve compatibility for plugins that do not need lifecycle behavior.

## Rejected

- Calling hosted-service `StartAsync` and `StopAsync` manually would create a second lifecycle implementation.
- Creating another service provider or service scope would split plugin dependencies from the application DI container.
- Ignoring lifecycle exceptions would allow the host to report a plugin as healthy when initialization failed.
- Using discovery order would make lifecycle behavior depend on filesystem or assembly enumeration.

## Consequences

Plugin lifecycle failures fail explicitly through the host startup/shutdown path. Plugin authors can use cancellation-aware hooks for initialization and cleanup, while long-running work belongs in `IHostedService` implementations returned by `GetHostedServices()`. Existing plugins that return no hosted services and implement no hooks remain valid through safe defaults.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery and contract boundary
- [ADR-016](./016-marten-and-wolverine-infrastructure.md) - host infrastructure lifecycle
- [Issue #10](https://github.com/AuthKits/AuthKit.Server/issues/10) - lifecycle and hosted-service requirements

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./023-plugin-application-pipeline-hooks.md) | [Next]()
