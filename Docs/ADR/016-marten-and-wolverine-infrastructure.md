[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./015-keycloak-external-jwt-authority.md) | [Next]()

# [ADR-016] Use Marten And Wolverine As Host Infrastructure

*2026-08* | Status: accepted

**Tag:** #adr_016

**Date:** 2026-08-26

**Scope:** Host.Configuration

## Context

The host needs document store for durable data (including the keystore from ADR-011 and plugin documents) and message/command handling backbone that can also discover handlers contributed by dynamically loaded plugins (ADR-010).

## Problem

Choosing persistence and messaging primitives affects every layer: plugins must be able to participate in the same command pipeline and document store, and validation should be part of message processing rather than scattered. Hand rolling dispatch or mixing ORMs complicates that integration.

## Decision

The host standardizes on Marten as the document store and Wolverine as the messaging/command backbone, integrated together:

- `ConfigureMarten` connects to Marten (connection string `Marten`), `AutoCreate.All` schema, lightweight sessions, and `IntegrateWithWolverine()`; `IDocumentSession` is exposed as scoped service.
- `ConfigureWolverine` uses Wolverine with FluentValidation integrated into message processing, and `IncludeEventHandlers(plugins)` so handlers from the Core assembly and dynamically loaded plugin assemblies are discovered.

### Design Rationale

- Marten + Wolverine integration lets document writes and message handling share transactions and one configuration story.
- Discovering handlers from plugin assemblies means solutions (eg. DevTokens) plug into the same command pipeline without host changes.
- FluentValidation in the Wolverine pipeline centralizes command validation before handlers run.

## Rejected

- Entity Framework Core or custom ORM as the primary store.
- A hand written command dispatcher instead of Wolverine.
- A separate event/ message store disconnected from the document store.

## Consequences

Plugins and Core share one persistence and messaging model, which keeps handlers, documents, and the keystore consistent. The cost is required Postgres/Marten dependency and the learning surface of two frameworks; infra logging for Wolverine/Marten/Npgsql is intentionally suppressed to reduce noise.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin handlers discovered by Wolverine
- [ADR-010](./010-plugin-loading-from-directory.md) - loaded assemblies feed the infrastructure
- [ADR-011](./011-keystore-persisted-as-singleton-marten-document.md) - keystore stored in Marten
- [ADR-012](./012-token-key-bindings-persisted-in-marten.md) - bindings stored in Marten

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./015-keycloak-external-jwt-authority.md) | [Next](./017-api-key-credential-extraction-strategies.md)
