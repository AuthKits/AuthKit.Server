[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./007-default-signing-algorithm-rsa-4096.md) | [Next](./009-dynamic-plugin-discovery.md)

# [ADR-008] Standardize API Errors Through Core Error Response Contract

*2026-08* | Status: accepted

**Tag:** #adr_008

**Date:** 2026-08-26

**Scope:** AuthKit.Core

## Context

Hosts (REST, gRPC, plugins) report failures to API clients and need consistent shape so consumers can parse errors programmatically. The core already define `DomainException` base for rule violations and an `ErrorMetadataOptions` for documentation links.

## Problem

Without shared error contract owned by the core, each host invents its own JSON schema, field names, and timestamp handling, fragmenting client error handling and breaking interoperability between API surfaces.

## Decision

The core owns standardized `ErrorResponse` DTO with `error` and `error_description` JSON fields (matching OAuth2 error naming) and UTC `timestamp`, `DomainException` base that hosts map onto it. Error documentation references are centralized in `ErrorMetadataOptions.DocsBaseUrl`.

### ErrorResponse

**Responsibilities:**

- Present stable, client facing error shape (`error`, `error_description`, `timestamp`).
- Remain serialization friendly and host agnostic.

### DomainException

**Responsibilities:**

- Act as the common base for domain rule violations.
- Give hosts one type to catch and translate into `ErrorResponse`.

### Design Rationale

- A single contract keeps client error parsing uniform across all hosts.
- Field names maximize compatibility with standard clients.
- The UTC timestamp aids correlation and auditing without host specific logic.

## Rejected

- Letting each host define its own error JSON shape.
- Returning raw exception text or stack traces to clients.
- Omitting timestamp or documentation link from the contract.

## Consequences

Clients get predictable error format and hosts share one translation path from domain failures. The cost is keeping `ErrorResponse` and `DomainException` in sync as new error cases appear.

## Related

- [ADR-014](./014-error-responses-via-middleware.md) - host renders this contract as ProblemDetails

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./007-default-signing-algorithm-rsa-4096.md) | [Next](./009-dynamic-plugin-discovery.md)
