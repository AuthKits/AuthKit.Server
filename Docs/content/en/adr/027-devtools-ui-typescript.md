[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./026-plugin-authentication-and-authorization-hooks.md) | [Next](./028-plugin-contract-and-dynamic-loading-architecture.md)

# [ADR-027] Compile The DevTools UI From Modular TypeScript Into A Single Embedded Resource

*2026-09* | Status: accepted

**Tag:** #adr_027

**Date:** 2026-09-16

**Scope:** Plugins.Solutions.DevTools

## Context

The DevTools plugin ships a web UI (Svelte 5 with Tailwind) for exploring gRPC services and invoking methods. Deploying that UI as loose static files would split every release into two artifacts that can drift apart: the server and the interface used to inspect it.

## Problem

A modular TypeScript application does not deploy itself as one file. Without a bundling step the UI would require a static-file hosting story (paths, caching, version matching) on top of the plugin assembly that already carries everything else.

## Decision

The UI builds with `vite build`, collapses to a single self-contained HTML file through `vite-plugin-singlefile`, and is finalized for embedding by `scripts/finalize-ui.mjs`. Type safety is enforced up front with `svelte-check`. The resulting resource travels inside the plugin assembly: one assembly, one UI, always matching the backend it ships with.

## Rejected

- Serving the UI as loose static files next to the plugin assembly.
- Versioning the UI independently from the server it inspects.

## Consequences

Deploying AuthKit never involves a separate static-file step, and the UI version cannot drift from the inspected server. The cost is build-time coupling: UI changes require rebuilding the plugin assembly.

## Related

- [ADR-020](./020-devtools-plugin.md) - DevTools plugin hosting the UI
- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery loading the assembly

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./026-plugin-authentication-and-authorization-hooks.md) | [Next](./028-plugin-contract-and-dynamic-loading-architecture.md)
