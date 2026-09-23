# Quick Start

Get AuthKit up and running in minutes — from zero to a verified token. If you prefer the big picture first, start with [Introduction](/guide/introduction/).

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed (the recommended path).
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — only for local development without Docker.
- Free ports: `5000`, `5001`, `8081`, `5433`, `5434`.

## Step 0: The .env File

All secret configuration lives in `.env`, which is never committed:

```bash
cp .env.example .env
```

Minimum to change before the first start:

- `DEV_CERT_PASSWORD` — password for the dev HTTPS certificate;
- `KEYCLOAK_CLIENT_SECRET` — Keycloak client secret.

The rest works on defaults. Full variable map: [Docker + Kestrel](/infrastructure/docker/).

## Option 1: Run with Docker (Recommended)

```bash
docker compose up --build
```

This starts AuthKit with PostgreSQL (Keycloak joins via the `keycloak` profile). The first start builds images and runs storage migration — it takes a while; later starts are fast.

### Ports

| Service | Port | Purpose |
|---------|------|---------|
| AuthKit REST | `5000` | REST API (HTTPS) |
| AuthKit gRPC | `5001` | gRPC API (HTTPS) |
| Keycloak | `8081` | Identity provider (HTTP) |
| PostgreSQL (AuthKit) | `5434` | AuthKit database |

### Keycloak Setup

1. Open http://localhost:8081
2. Login with admin/admin
3. Import the realm from `Deploy/Keycloak/realms/realm-authz.json`
4. Confirm the `authz` realm exists with the `workspace-authz` client

Without the imported realm, developer sign-in won't work — the most common cause of 401s on a fresh stack.

## Option 2: Run Locally

For development without Docker:

```bash
# Restore dependencies
dotnet restore AuthKit.slnx

# Build
dotnet build AuthKit.slnx --configuration Release

# Run the host
dotnet run --project src/Host/Host.csproj
```

Default listen address: `http://0.0.0.0:8080` (HTTP/2). PostgreSQL and Keycloak must be reachable separately — easiest is to run just the databases from compose and the host locally.

For code iteration use the watcher:

```bash
dotnet watch run --project src/Host/Host.csproj
```

## Verify Everything Works

Check the health endpoint:

```bash
curl -k https://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234560",
  "componentDurations": {
    "AuthKitPlugins": "Healthy",
    "Marten": "Healthy"
  }
}
```

Also glance at `docker compose ps` — every service should be `healthy` or `running`.

## First End-to-End Session

With health green, close the full loop in 5 minutes:

1. Open Swagger on the running host: `https://localhost:5000/devtools/swagger`.
2. Sign in to Keycloak (http://localhost:8081, `admin` / `admin`) and copy your access token.
3. In Swagger, call `POST /sdk/developer-tokens` with the Keycloak token in `Authorize` — you get a developer JWT plus an `rk_live_…` key.
4. Call `POST /sdk/tokens/verify` with that JWT — a `"valid": true` response closes the loop.

Only now do you have the working chain: Keycloak → AuthKit → SDK token → verification.

## Stopping and Cleaning

```bash
docker compose stop          # stop, everything stays
docker compose down          # stop, containers gone, data stays
docker compose down -v       # stop + wipe volumes (fresh database, new keys!)
```

:::warning[down -v wipes keys]
After dropping volumes the keystore is new — previously issued tokens stop verifying.
:::

## Troubleshooting

- **Containers are up but health doesn't answer** — wait for `healthy` in `docker compose ps`; `auth` only starts after a healthy database.
- **Keycloak empty after startup** — the `keycloak` profile wasn't enabled (`docker compose --profile keycloak up`) or the realm file is missing.
- **Certificate error on startup** — wrong `DEV_CERT_PASSWORD` or missing `./certs`; read the `auth` logs.
- **401 from Swagger** — missing or wrong Keycloak token in `Authorize`; SDK tokens don't replace the Keycloak bearer.
- **Port in use** — something sits on `5000`/`8081`/`5434`; stop it or remap the left side of `ports`.
- **Clean reset** — `docker compose down -v` wipes the databases and starts everything from zero (tokens go too).

## Next Steps

1. [Configure Keycloak](/guide/configuration/)
2. [Create a developer token](/reference/rest-api/)
3. [Explore the REST API](/reference/rest-api/)
4. [Docker + Kestrel](/infrastructure/docker/) — full stack lifecycle
