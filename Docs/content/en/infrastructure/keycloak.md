# Keycloak

Keycloak is the external JWT authority developers sign in through. AuthKit never stores developer passwords it validates Keycloak-issued access tokens and mints its own SDK tokens from them. See [ADR-015](/adr/015-keycloak-external-jwt-authority/).

## How It Works

The flow has three steps with a clean split of responsibilities:

1. **Sign in** the developer authenticates with Keycloak through a standard OIDC flow and receives a JWT (bearer).
2. **Validate** the host validates the token via ASP.NET Core JWT bearer against the realm (`AddKeycloakServices`): issuer and audience must match, `NameClaimType` is `preferred_username`.
3. **Roles** an `OnTokenValidated` hook maps the client's `resource_access` roles into standard `ClaimTypes.Role` claims, so ordinary authorization policies work with no Keycloak-specific code in handlers.

Rejected alternatives: local user store, self issued host tokens, custom role database all of them would couple the host to identity logic it should not own.

## Setup

1. Open http://localhost:8081, log in with `admin` / `admin`.
2. Import the realm from `Deploy/Keycloak/realms/realm-authz.json` (mounted read-only with auto-import in compose).
3. Set the client secret in `.env` (`KEYCLOAK_CLIENT_SECRET`).

## Environment

| Variable | Meaning | Default |
|----------|---------|---------|
| `KEYCLOAK_URL` | Keycloak base URL (`Authority`) | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Keycloak realm | `authz` |
| `KEYCLOAK_CLIENT_ID` | Keycloak client ID (`Audience`) | `workspace-authz` |
| `KEYCLOAK_CLIENT_SECRET` | Keycloak client secret | _required_ |

The same configuration carries dev, test, and production values change, code doesn't.

## Using the Token

Every API call carries the Keycloak token in the header:

```http
Authorization: Bearer <keycloak-access-token>
```

From it, AuthKit mints its own developer tokens (`X-Developer-Token`) details in the [REST API Reference](/reference/rest-api/).

## Dev vs Production

Deliberate development concessions, to be tightened before production:

- `RequireHttpsMetadata = false`;
- a backchannel with `DangerousAcceptAnyServerCertificateValidator`.

:::danger[Before production]
Both of the above must be tightened before any production deployment otherwise tokens travel over HTTP and certificate validation doesn't exist.
:::

## Troubleshooting

- **401 with a valid-looking token** check `KEYCLOAK_URL`/`KEYCLOAK_REALM`/`KEYCLOAK_CLIENT_ID`: issuer or audience doesn't match the token's issuer.
- **Missing roles in claims** roles must live in the client's `resource_access`, not at realm level the hook only maps client roles.
- **No realm after startup** the `keycloak` profile wasn't enabled or the realm file wasn't mounted see [Docker + Kestrel](/infrastructure/docker/).

Full settings reference: [Configuration](/guide/configuration/).
