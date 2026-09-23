[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./017-api-key-credential-extraction-strategies.md) | [Next](./019-plugin-metadata-attribute.md)

# [ADR-018] Extend The Security Scheme Contract With Explicit Host And OpenAPI Handling

*2026-09* | Status: accepted

**Tag:** #adr_018

**Date:** 2026-09-10

**Scope:** AuthKit.Plugins.Abstractions

## Context

Plugins expose authentication mechanisms as transport agnostic `AuthKitSecuritySchemeDescriptor` metadata (ADR-009). The original contract enum `AuthKitSecuritySchemeType` covered only `ApiKey`, `Http`, `OAuth2`, and `OpenIdConnect` where `OpenIdConnect` was an alias of `OAuth2` sharing numeric value `2`. `AuthKitApiKeyLocation` covered `Header`, `Query`, and `Cookie`. Any other mechanism (mutual TLS, sessions, plugin defined schemes, HTTP Basic) or transport (gRPC metadata, request body) had to be approximated with strings or overloaded existing values.

## Problem

Approximation destroys type safety and lets the host confuse distinct mechanisms: session authentication with an API key carried in a cookie, HTTP Basic with generic HTTP authentication, gRPC metadata with HTTP headers, and body credential with one in a header or query parameter. In addition, the bundled OpenAPI serializer (Swashbuckle 9.x / Microsoft.OpenApi 1.6.x) cannot represent mutual TLS, sessions, custom schemes, gRPC metadata, or body locations in its OpenAPI 3.0 output. Substituting a generic scheme silently would not only hide misconfiguration, it would produce misleading documentation for clients and generated SDKs.

## Decision

Both enums grow additively, and every value is handled explicitly by the host and by the OpenAPI mapper:

- `AuthKitSecuritySchemeType` gains `MutualTls = 3`, `Session = 4`, `Custom = 5`, `Basic = 6`. Existing values stay untouched (`ApiKey = 0`, `Http = 1`, `OAuth2 = 2`). The historical alias is removed: `OpenIdConnect` becomes a distinct value `7` with the deviation documented in its remarks renumbering existing values is forbidden because numeric values are part of the plugin contract (`ApiKey`..`Basic` already occupy `0`..`6`).
- `AuthKitApiKeyLocation` gains `GrpcMetadata = 3` and `Body = 4` existing values stay untouched.
- Numeric values are declared part of the plugin contract in both enums: they must never be reused or renumbered.
- The host enumerates its capabilities as `PluginContractValidator.SupportedSchemeTypes` and `SupportedApiKeyLocations`. `PluginContractValidator.Validate` checks every declared scheme type and location against these sets: supported values pass defined but unsupported and unknown (future) values raise `InvalidPluginContractException`. Unknown values are identified by their numeric identity and never resolved to `Custom` or any known value.
- `AuthKitOpenApiSecuritySchemeMapper` maps every value explicitly: semantically correct OpenAPI representations for `ApiKey`, `Http`, `OAuth2`, `OpenIdConnect`, and `Basic` (as HTTP authentication with scheme `basic`); `NotSupportedException` for values with no correct 3.0 representation (`MutualTls`, `Session`, `Custom`; `GrpcMetadata` and `Body` as API key locations); `ArgumentOutOfRangeException` for unknown values.
- Runtime support and OpenAPI representability are orthogonal: the host supports `GrpcMetadata` and `Body` credential extraction at runtime (ADR-017) even though OpenAPI 3.0 cannot describe them. `RestfulConfiguration` catches mapper failures and logs warning while skipping the definition it never emits a generic substitute and never crashes document generation.
- `Custom` should be accompanied by an optional plugin-supplied `Description`; a missing description produces a validation warning (never a mapping to a built-in scheme).
- The host only accepts `OpenApi:SpecVersion` `3.0`. `3.1` which would be needed for a real `mutualTLS` representation is rejected with `InvalidOperationException` instead of silently emitting 3.0 document.

### Design Rationale

- **Additive contract**: old plugins (including `DevTokens`) compile and load unmodified, and persisted descriptors keep their meaning.
- **Explicit over clever**: every contract value has a named case in exactly two places (host capability check and OpenAPI mapper), forcing authors to decide support or rejection for each new value.
- **Fail fast**: an unsupported scheme fails plugin validation at startup non representable one is omitted from Swagger with a logged reason.
- **No fake OpenAPI**: emitting `apiKey` for mutual TLS or `Header` for gRPC metadata would generate plausible looking but wrong client contracts.

## Rejected

- Renumbering existing enum values to match cleaner sequence breaks every shipped plugin and persisted descriptor forbidden by the must not change rule.
- Keeping the `OpenIdConnect` = `OAuth2` alias two distinct mechanisms cannot share numeric identity a plugin declaring an OIDC flow must not deserialize as OAuth2.
- Generic fallback in the mapper (unknown -> `Custom`, `MutualTls` -> HTTP, `GrpcMetadata` -> `Header`, `Body` -> `Header`/`Query`/`Cookie`) hides misconfiguration and emits misleading documentation.
- Automatically upgrading generated documents to OpenAPI 3.1 to gain `mutualTLS` the bundled serializer stack has no 3.1 support the host rejects the setting rather than silently downgrading.
- Treating `Session` as API-key-in-cookie and `Basic` as generic HTTP authentication reproduces the ambiguity this ADR removes.

## Consequences

Adding new authentication mechanism or transport is now additive: define the value, extend the host capability set, add mapper case, and cover it with positive and positive/negative no fallback tests. The OpenIdConnect value sits outside the natural `0`..`6` sequence (documented in the enum remarks). Plugins that declare unsupported mechanisms fail contract validation at startup per host extensions use `PluginContractValidator.CreateCustom`. Swagger output for plugin mixing representable and non representable schemes contains only the representable definitions, with logged warning per skipped scheme. Two explicit contract guarantees now exist: no silent fallback between schemes or locations, and unknown future values are always rejected.

## Related

- [ADR-009](./009-dynamic-plugin-discovery.md) - plugin security scheme descriptors
- [ADR-017](./017-api-key-credential-extraction-strategies.md) - runtime extraction for header/query/cookie/gRPC metadata/body
- [ADR-013](./013-dual-rest-and-grpc-transport.md) - gRPC transport surface

[ADR Home](../../README.md) | [Category Index](./README.md) | [Previous](./017-api-key-credential-extraction-strategies.md) | [Next](./019-plugin-metadata-attribute.md)