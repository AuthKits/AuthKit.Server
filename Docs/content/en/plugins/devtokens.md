# DevTokens

The `DevTokens` plugin (`authkit.devtokens`) issues, validates, and authorizes developer tokens for SDK access. Signing keys and token-to-key bindings come from the Host core the plugin owns the token lifecycle, not the key infrastructure.

## Token Lifecycle

1. **Create** `POST /sdk/developer-tokens` with name, scopes, and lifetime. Returns a signed JWT plus a `rk_live_…` key.
2. **Persist** tokens are stored in Marten with metadata (scopes, expiry).
3. **Verify** `POST /sdk/tokens/verify` validates without a database round-trip where possible.
4. **Revoke & rotate** `POST /sdk/tokens/{tokenId}/revoke-rotate` replaces a token in one call.

Full endpoint shapes: [REST API Reference](/reference/rest-api/).

## Scopes

Tokens carry scopes (eg. `write:packages`). Requests are authorized against the scopes the token was issued with a token never grows permissions beyond its issue set.

## Using Tokens

SDK calls present two credentials:

```http
Authorization: Bearer <keycloak-token>
X-Developer-Token: <authkit-jwt>
```

The pipeline validates both and enforces scope access. See [Using Tokens in SDK Requests](/reference/rest-api/#Using-Tokens-in-SDK-Requests).

## Validation

Request models are validated with FluentValidation before anything touches the store malformed payloads fail fast with RFC 7807 problem details.
