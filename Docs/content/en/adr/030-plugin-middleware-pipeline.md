[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./029-structured-plugin-health-contract.md) | [Next]()

# [ADR-030] Declare Plugin Middleware As A First-Class Transport-Explicit Pipeline

*2026-09* | Status: accepted

**Tag:** #adr_030

**Date:** 2026-09-24

**Scope:** AuthKit.Plugins.Abstractions + Host + PluginContractValidator

## Context

ADR-023 gave plugins explicit pipeline hooks (`ConfigureApplication`, `ConfigurePipeline`, `MapEndpoints`) plus the legacy single `MiddlewareType` slot. PR #51 extends this with declarative middleware entries: plugins register multiple `PluginMiddleware` records carrying an explicit `Transport` (`Http`/`Grpc`), a semantic `PipelinePosition`, an `Order` key, and an enabled flag, while the Host owns composition and validation. This ADR records why the pipeline is declarative and transport-explicit rather than reflective or bridging.

## Problem

A single implicit middleware slot cannot express ordering across plugins, per-entry enablement, or transport differences between HTTP middleware and gRPC interceptors. Guessing transport by reflection (e.g. "is this an `Interceptor`?") hides configuration errors, and bridging `HttpContext` middleware into gRPC calls corrupts the gRPC framing. The old `MiddlewareType` slot was therefore removed instead of kept as a compatibility path.

## Decision

- Plugins declare *what* via `PluginMiddleware` (type-only declaration, no factory): `MiddlewareType`, semantic `Position` (`BeforeRouting` … `AfterEndpointExecution`), `Order`, `IsMiddlewareEnabled`, `Name`, explicit `Transport`.
- The Host owns *activation and composition*: HTTP entries compose per position in deterministic order (`Order → PluginId → declaration index`) `IAuthKitMiddleware` / `AuthKitMiddlewareBase` resolve per request from the request service provider convention types go through `UseMiddleware`. gRPC entries must be `Interceptor` subclasses composed into the native interceptor chain — no second interceptor framework, no `HttpContext` bridge.
- HTTP middleware never runs on gRPC calls (skipped by content type branch) HTTP only entries are logged as skipped with guidance to declare `Transport = Grpc`.
- The `PluginContractValidator` (`MiddlewareRule`) fails fast on structural violations: ambiguous AuthKit models, static/void `Invoke`, multi-ctor convention types, generic/abstract types, `Transport`/`MiddlewareType` mismatches, undefined `Transport`/`Position` values. Disabled entries are still validated so contracts cannot be hidden by disabling.
- There is no legacy single slot entry point: the old `MiddlewareType` property and its host compatibility path were removed. All middleware is declared via `Middlewares`.

### Design Rationale

- Explicit `Transport` removes reflection guessing: a misdeclared entry fails with a diagnostic naming plugin and type instead of silently running on the wrong transport.
- Semantic positions stay transport-neutral in intent (`AfterAuthorization` means after authentication *and* authorization) while the Host maps them per transport.
- Deterministic ordering (`Order → PluginId → index`) makes equal-position composition reviewable and testable, independent of discovery order.
- Per request DI resolution shares single scope, so scoped services behave consistently inside plugin middleware.

## Rejected

- Reflection-based transport inference: accepts misdeclared entries and produces runtime failures far from the declaration site.
- `HttpContext` to gRPC bridging: corrupts gRPC framing for middleware that writes or short-circuits the response.
- A second interceptor framework beside `Grpc.Core.Interceptors.Interceptor`: duplicates the native chain for no behavioral gain.
- Silently dropping undefined `Transport`/`Position` or unvalidated disabled entries: hides plugin configuration errors (CodeRabbit review on PR #51 caught exactly this).
- Keeping the old single `MiddlewareType` slot alongside `Middlewares`: one implicit slot cannot express ordering, per-entry enablement, or transport, and dual paths risk duplicate registration and divergent validation. DevTokens and DevTools were migrated to `BeforeAuthentication` declarative entries.

## Consequences

- Plugin authors must declare transport explicitly and choose supported semantic position invalid declarations fail at validation/startup instead of misbehaving at runtime.
- HTTP middleware that previously also ran on gRPC calls no longer does plugins needing gRPC behavior must ship an `Interceptor`.
- There is no backward-compatible middleware slot: existing plugins must migrate `MiddlewareType` to a `Middlewares` entry (DevTokens/DevTools use `BeforeAuthentication`) undeclared middleware no longer runs.
- Validator and host rules must evolve together: any new middleware model needs both a validation branch and a host activation path.

## Related

- [ADR-023](./023-plugin-application-pipeline-hooks.md) - explicit host pipeline hooks and legacy middleware slot
- [ADR-013](./013-dual-rest-and-grpc-transport.md) - dual REST and gRPC transport surfaces
- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin discovery and contract boundary

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./029-structured-plugin-health-contract.md) | [Next]()
