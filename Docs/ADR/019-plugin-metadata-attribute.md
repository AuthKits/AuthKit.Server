[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./018-security-scheme-contract-explicit-handling.md) | [Next](./020-devtools-plugin.md)

# [ADR-019] Declare Plugin Identity Through The PluginMetadata Attribute

*2026-09* | Status: accepted

**Tag:** #adr_019

**Date:** 2026-09-11

**Scope:** AuthKit.Plugins.Abstractions

## Context

Every `IAuthKitPlugin` implementation (ADR-009) exposed its identity by overriding `Name`, `Version`, and `Description` properties in code. The DevTokens and DevTools plugins duplicated that boilerplate, and richer cataloging information stable machine id, tags, capabilities, dependencies, author, license, repository URL had no contract surface at all.

## Problem

Repeating identity as c# code properties spreads the plugin's identity across the class body, makes version bumps code edit, and leaves no single structural place where future tool (loader diagnostics, admin UI) can read identity and cataloging metadata. Adding cataloging metadata would require new members on the contract interface, forcing every existing plugin to implement them.

## Decision

Plugin identity and cataloging metadata move to `[PluginMetadata]` attribute on the plugin class:

- `PluginMetadataAttribute` (namespace `AuthKit.Plugins.Abstractions.Contracts.Plugins`) declares `Id`, `Version`, `Name`, `Tags`, `DependsOn`, `Capabilities`, `DisplayName`, `Description`, `Author`, `License`, `LicenseUrl`, `Homepage`, and `RepositoryUrl`. The constructor is `(id, version, name, ...)` with all fields after `name` optional.
- `IAuthKitPlugin.Name`, `Version`, and `Description` are now **default interface members** that read the attribute through a private helper `GetPluginMetadata()`. Plugins no longer need to implement them: `Name` falls back to the type name and `Version` to `0.0.0` when the class carries no attribute.
- Plugins may still override the three properties if they need computed identity, but the attribute is the preferred declaration point. The two shipped plugins (`DevTokens`, `DevTools`) declare their identity exclusively via `[PluginMetadata]`.
- `AttributeUsage` is `Class`, `Inherited = false`, `AllowMultiple = false` one attribute per plugin class.
- Array metadata (`Tags`, `DependsOn`, `Capabilities`) is exposed as `IReadOnlyList<string>` and defaults to an empty list when absent, so consumers never see `null`.

### Design Rationale

- **Single declaration point**: identity and cataloging live in one attribute, next to the class it describes.
- **Backward compatible**: default interface members keep old plugin classes compilable and loadable no host, plugin, or `PluginLoader` change was required for discovery or startup output.
- **Additive cataloging**: tags/capabilities/dependencies/authoring metadata gains contract surface without touching the interface's member list (no breaking change to existing implementations).
- **Declarative over imperative**: metadata as an attribute is readable, discoverable via reflection, and usable by tools that never instantiate the plugin class.

## Rejected

- Adding `Tags`, `Capabilities`, `Dependencies`, etc. as new abstract members on `IAuthKitPlugin` breaks every existing plugin implementation (compile time contract break).
- Keeping identity purely as code properties side by side with separate cataloging attribute splits identity across two mechanisms.
- A metadata file (JSON/embedded resource) sidecar loses compile time checking and reflection discoverability for little gain.

## Consequences

New plugins declare one attribute and get correct `Name`/`Version`/`Description` for free; the host's plugin loader consumes the attribute through the default interface members, so `ServerHost` startup output ("Loaded plugin 'DevTools' v1.0.0") reflects attribute values. Cataloging fields are available to future administrative surfaces without further contract changes. Because default interface members can be overridden, a plugin that needs computed identity keeps that freedom but the two shipped plugins no longer use it.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - the `IAuthKitPlugin` contract extended by this ADR
- [ADR-010](./010-plugin-loading-from-directory.md) - the loader that reads plugin identity at startup

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./018-security-scheme-contract-explicit-handling.md) | [Next](./020-devtools-plugin.md)