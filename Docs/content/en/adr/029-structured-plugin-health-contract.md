[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./028-plugin-contract-and-dynamic-loading-architecture.md) | [Next]()

# [ADR-029] Expose Structured And Cancellable Plugin Health Results

*2026-09* | Status: accepted

**Tag:** #adr_029

**Date:** 2026-09-13

**Scope:** AuthKit.Plugins.Abstractions + Host

## Context

Plugins may depend on several independent components, such as database, cache,
external API, or message queue. A single boolean health value cannot preserve
which component is degraded or why check failed. Health checks may also perform
asynchronous I/O and need to stop when the request or host is cancelled.

## Problem

The original plugin contract returned `Task<bool> CheckHealthAsync(IServiceProvider)`.
That shape loses intermediate health states, diagnostic information, multiple
observations, and cancellation. Replacing it without a migration path would
break existing plugin implementations.

## Decision

`IAuthKitPlugin.CheckHealthAsync` returns:

```csharp
Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
    IServiceProvider services,
    CancellationToken cancellationToken = default)
```

`PluginHealthResult` contains the strongly typed `PluginHealthStatus`, an optional
reason, and optional plugin owned diagnostic data. `Healthy`, `Degraded`, and
`Unhealthy` remain distinct. A plugin may return one result or multiple results,
with each result representing an independent observation.

The default interface implementation returns one `Healthy` result so plugins that
do not provide custom check remain valid. The host preserves the result list and
uses the supplied request cancellation token. Cancellation is propagated rather
than converted into fabricated health result.

## Rejected

- Keeping `bool` would discard degraded state and diagnostics.
- Collapsing multiple results inside the plugin would make host aggregation lossy.
- Inferring status from `Reason` or `Data` would make the contract weakly typed.
- Silently replacing cancellation with `Healthy` or `Unhealthy` would hide an
  incomplete check.
- Adding second health method would leave two competing public contracts.

## Consequences

Health consumers must handle list of structured results and define aggregation
explicitly. The host health endpoint reports the highest severity status while
preserving every plugin result in the response. Plugin-specific diagnostic keys
remain extensible, but they do not override `Status`.

The change is source breaking for plugins that implement the old `Task<bool>`
method; shipped plugins and the manifest example are migrated together. The
contract validator invokes the new method and rejects an empty result collection.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin contract and dynamic loading
- [ADR-024](./024-plugin-lifecycle-and-hosted-services.md) - plugin lifecycle integration
- [Issue #14](https://github.com/AuthKits/AuthKit.Server/issues/14) - structured plugin health result
- [Issue #15](https://github.com/AuthKits/AuthKit.Server/issues/15) - multiple results and cancellation

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./028-plugin-contract-and-dynamic-loading-architecture.md) | [Next]()
