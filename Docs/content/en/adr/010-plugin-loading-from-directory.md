[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./009-dynamic-plugin-discovery.md) | [Next](./011-keystore-persisted-as-singleton-marten-document.md)

# [ADR-010] Load Plugins From Configurable Directory At Startup

*2026-08* | Status: accepted

**Tag:** #adr_010

**Date:** 2026-08-26

**Scope:** Host.Plugins

## Context

The host must bring plugin packages online without compile time reference (see ADR-009). The loading step has to happen early enough that infrastructure configured afterwards Wolverine, Marten, MVC, can see the plugin assemblies while it builds its own configuration.

## Problem

If plugins are loaded after the host is built, their handlers, controllers, and DI registrations are invisible to the frameworks that already scanned assemblies. The host also needs predictable, low ceremony way to locate each plugin's entry assembly and fail safely when folder is malformed.

## Decision

`PluginLoader.LoadPlugins` discovers plugins from configurable `PluginsPath` (default `<base>/plugins`) **before** `WebApplicationBuilder.Build()` is called. Each plugin lives in its own subdirectory whose name must match its entry assembly (`<name>.dll`); the loader reflects public, non-abstract `IAuthKitPlugin` with parameterless constructor, instantiates it, and records it as `LoadedPlugin` (contract + assembly + directory). Assemblies load into `AssemblyLoadContext.Default` (not an isolated context) so framework and package types are shared across the boundary, and hot reload is explicitly not supported.

### PluginLoader

**Responsibilities:**

- Enumerate plugin subdirectories and resolve each entry assembly's dependencies.
- Validate and instantiate the `IAuthKitPlugin` implementation.
- Return only successfully loaded plugin, log and skip invalid folders.

### LoadedPlugin

**Responsibilities:**

- Carry the contract instance, assembly, and source directory for later host use (DI, Wolverine/MVC assembly discovery).

### Design Rationale

- Loading before `Build` lets Wolverine, Marten, and MVC discover plugin types during configuration.
- The default load context shares runtime types (Wolverine `IMessageBus`, Marten `IDocumentSession`, MVC types) across the boundary, avoiding type-identity bugs.
- A naming convention plus graceful skip keeps deployment simple and resilient to broken plugin folder.

## Rejected

- Loading plugins into isolated `AssemblyLoadContext`s (type-identity mismatches with shared frameworks).
- Loading after the host is built (infrastructure could not see plugin assemblies).
- Supporting hot swapping or unloading of plugins at runtime.
- Requiring manifest file beyond the directory/assembly naming convention.

## Consequences

Plugins are online before infrastructure is configured and participate uniformly in DI, messaging, and MVC. The cost is no runtime isolation or reload, and the host trusts plugin assemblies loaded into the default context, so plugin provenance must be controlled operationally.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - the plugin contract being loaded
- [ADR-016](./016-marten-and-wolverine-infrastructure.md) - loaded assemblies feed Wolverine/Marten

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./009-dynamic-plugin-discovery.md) | [Next](./011-keystore-persisted-as-singleton-marten-document.md)
