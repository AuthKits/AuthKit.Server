[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./030-plugin-middleware-pipeline.md) | [Next]()

# [ADR-031] Discover Plugins Through Manifests With A Pre-Load Compatibility Gate

*2026-09* | Status: accepted

**Tag:** #adr_031

**Date:** 2026-09-24

**Scope:** AuthKit.Plugins.Abstractions + Host plugin loading

## Context

Plugins were loaded by single static host routine that located an entry assembly, constructed the instance, and only then read metadata from the instance itself. The host could not reason about a plugin before constructing it, so a plugin requiring a newer host failed late (or crashed startup), disabled plugins were still constructed, and duplicate identities surfaced far from their cause. There was no seam for custom discovery or loading.

## Problem

Metadata lived only on the constructed `IAuthKitPlugin` instance, which forced the host to build the object before any compatibility decision. A single `LoadPlugins` routine mixed discovery, compatibility, construction, and validation, so none of those stages could be replaced, tested, or reasoned about in isolation.

## Decision

- Split the pipeline into swappable stages with explicit ownership: `IPluginDiscoverer` finds candidates and reads manifests (never activates), `IPluginLoader` loads assemblies and constructs instances (never judges compatibility), and the host pipeline owns validation, gating, consistency, and activation around them.
- The discoverer reads `plugin.json` / `plugin.manifest` / `manifest.json` from disk without loading assemblies, yielding `DiscoveredPlugin` (manifest + opaque location + discovery error). Order per stage: discovery errors → structural validation → duplicate Id rejection → compatibility gate → load → manifest/instance consistency → contract validation.
- The compatibility gate runs pre-load on the manifest: `IsEnabled == false` skips quietly, `HostVersion < MinHostVersion` hard-rejects (no warn-only — a too-new plugin risks `MissingMethodException` / `TypeLoadException`). Undefined `MinHostVersion` means unaffected.
- Manifest/instance consistency (`Id`, `Name`, `Version`, `IsEnabled`, set-equal `Capabilities`, `MinHostVersion`, `DependsOn`) is a hard failure. Duplicate manifest Ids are rejected deterministically (first by location wins).
- Outcomes are captured in the host-internal `PluginLoadResult` (loaded / skipped-disabled / rejected / invalid with reasons) for startup logs and diagnostics; it is deliberately not part of the plugin contract.
- A manifest is required: directories without a readable manifest are invalid and never load. There is no legacy fallback. `PluginManifest` never inherits `IAuthKitPlugin` — shared semantics, separate models.

### Design Rationale

- Reading metadata before loading moves failures (bad manifest, duplicate Id, too-new plugin, disabled plugin) ahead of assembly loading, where they are cheap and diagnosable.
- Opaque `Location` keeps the contract source-agnostic (directory today, feed or package tomorrow) without leaking loader details into discovery.
- Per-candidate loader attribution makes every outcome explainable in `PluginLoadResult` instead of failing the whole batch or crashing startup (the previous behavior on contract violation).
- The loader stays dumb on purpose: compatibility policy lives in exactly one place (the gate + pipeline), so custom loaders cannot silently change acceptance rules.

## Rejected

- Keeping the monolithic static loader: no seam for custom discovery/loading, untestable stages, late failures.
- Warn-only compatibility mode: loading a plugin built for a newer host corrupts behavior instead of degrading gracefully; policy engines (`Strict`/`Warn`/`Ignore`) belong to a future host feature, not the basic gate.
- Max-host-version ceiling and per-feature capability negotiation now: reserved for future gate rules.
- Coupling manifest to instance via inheritance: binds the pre-activation model to runtime construction, defeating the purpose of the gate.

## Consequences

- Every plugin solution must ship its `manifest.json` (committed like Shield and Example, or generated at build like DevTokens and DevTools via `AuthKit.ManifestGenerator`, which the Dockerfile runs after publish) — without it the plugin is rejected at startup.
- Every plugin solution should commit or generate its `manifest.json` (Shield and Example do; DevTokens/DevTools generate theirs at build) — otherwise the gate cannot see it.
- `SemanticVersion` (SemVer 2.0.0, build metadata ignored for precedence) is the only version comparison; `System.Version` must never be used.
- Future gate rules (capabilities, platform, max version) plug into `CompatibilityGate` without touching discovery or loading.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery and contract boundary
- [ADR-010](./010-plugin-loading-from-directory.md) - plugin loading and legacy middleware slot
- [ADR-028](./028-plugin-contract-and-dynamic-loading-architecture.md) - plugin contract and dynamic loading architecture

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./030-plugin-middleware-pipeline.md) | [Next]()
