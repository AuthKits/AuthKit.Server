[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./031-plugin-discovery-manifest-gate.md) | [Next]()

# [ADR-032] Isolate Plugins In Per-Plugin Load Contexts With Deterministic Ordering

*2026-09* | Status: accepted

**Tag:** #adr_032

**Date:** 2026-09-29

**Scope:** Host plugin loading (isolation, ordering, discovery cache)

## Context

All plugins loaded into the default `AssemblyLoadContext`, so conflicting transitive dependencies collapsed into one universe: whichever copy won broke the others with `TypeLoadException` or silent wrong-version binds. Load order followed discovery order, so dependencies routinely lost to dependents. Every restart paid full rediscovery.

## Problem

One shared context cannot host conflicting dependency versions, and filesystem order is not a loading order. Without isolation, adding any plugin risks every other plugin; without ordering, dependency edges are luck; without caching, startup redoes all discovery work including assembly probing.

## Decision

- Each plugin loads into its own collectible `AssemblyLoadContext` (`PluginLoadContext`), attached to the contract `LoadedPlugin` — never to `DiscoveredPlugin`. Collectible enables future unload orchestration; the loader itself never unloads.
- Sharing wins by rule, in order: explicit shared contracts (`AuthKit.Plugins.Abstractions`, `Grpc.Core.Api`, `Google.Protobuf`), anything already loaded by default, anything shipped in the host application directory. Everything else resolves privately from the plugin directory. A plugin therefore always sees the host's `Core` types (DI identity holds) and never its own `Interceptor` copy.
- Accepted plugins load in Kahn topological order: dependencies first, ties by `Priority` ascending then registration order, never re-sorted afterward. Unknown dependency ids and cycles are startup errors; dependents of unavailable plugins are rejected as dependency-unavailable with propagation.
- Discovery results cache in a host-local file (`PluginManifest` + location + source fingerprint + schema version; never load contexts, types, or instances). Stale or corrupt cache is a miss, never a failure.
- The compatibility gate reads the effective enabled flag (manifest `IsEnabled` anded with host config, which may disable but never re-enable) before ordering, so disabled plugins are never ordered, isolated, or loaded.

### Design Rationale

- Returning null (fall back to default) for shared assemblies keeps one type universe for contracts while private universes diverge per plugin — the exact property DI and `is` checks need.
- The host-directory rule (not just already-loaded) closes the startup-ordering hole: loading runs before first gRPC/DI use, so an explicit list alone would still duplicate copies on a cold host.
- Kahn with priority-then-registration is deterministic for a given discovered set and reviewable in logs; discovery order alone stays the final tiebreak, never the strategy.
- Cache stores fingerprints, not results trust: any input change (manifest or dll bytes) invalidates, so stale entries cannot load.

## Rejected

- Single shared context with version unification: forces one dependency version on all plugins and the host — the original problem.
- Copying host assemblies per plugin directory: duplicates contract types per ALC and breaks DI identity silently.
- Ordering by discovery order or full re-sort by priority afterward: the former is nondeterministic, the latter breaks dependency edges.
- Distributed or in-memory-only cache: host-local file survives restarts with zero infrastructure; caching activated instances is explicitly out.

## Consequences

- Plugin solutions must not rely on privately loading assemblies the host ships; host-owned wins by rule and the plugin gets the host copy.
- Every plugin directory still needs its manifest (ADR-031); ordering and validation keys off manifest identity.
- `PluginLoadResult` distinguishes terminal outcomes (loaded / skipped-disabled / rejected / invalid) so each candidate is explainable.
- Future work (unload orchestration, max-version ceiling, capability rules) plugs into `PluginLoadContext`, `CompatibilityGate`, or `DependencyGraph` without touching discovery.

## Related

- [ADR-031](./031-plugin-discovery-manifest-gate.md) - manifest discovery with pre-load compatibility gate
- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery and contract boundary
- [ADR-028](./028-plugin-contract-and-dynamic-loading-architecture.md) - plugin contract and dynamic loading architecture

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./031-plugin-discovery-manifest-gate.md) | [Next]()
