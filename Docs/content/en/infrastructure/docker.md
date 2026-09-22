# Docker + Kestrel

The whole stack runs from one compose file: AuthKit, PostgreSQL, and Keycloak. This page walks the full lifecycle first run, daily use, debugging, and what changes on the way to production.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) with Compose v2.
- Ports `5000`, `5001`, `8081`, `5433`, `5434` free on your machine.
- A dev HTTPS certificate in `./certs` (see [Certificates](#certificates)).

## First Run

```bash
cp .env.example .env
docker compose up --build
```

What happens, in order:

1. Images build (`auth` from the root `Dockerfile`, `keycloak` from `Deploy/Keycloak/Dockerfile`) databases pull `postgres:15`.
2. Named volumes `AuthDev-data` and `Auth-data` are created your data survives container restarts.
3. `authdev-db` must report `healthy` (`pg_isready`) before `auth` even starts.
4. `auth` runs storage migration, maps REST + gRPC endpoints, and starts listening.

Verify with the health endpoint:

```bash
curl -k https://localhost:5000/health
```

## Services

| Service | Image / Build | Ports | Purpose |
|---------|---------------|-------|---------|
| `auth` | `Dockerfile` (repo root) | `5000` REST, `5001` gRPC | AuthKit host (HTTPS) |
| `authdev-db` | `postgres:15` | `5434` → `5432` | AuthKit database (`AuthDev-data` volume) |
| `keycloak` | `Deploy/Keycloak/Dockerfile` | `8081` → `8080` | Identity provider, realm auto-imported |
| `keycloak-db` | `postgres:15` | `5433` → `5432` | Keycloak database (`Auth-data` volume) |

`auth` mounts `./certs` for the dev HTTPS certificate and reads `.env` plus `ConnectionStrings__DefaultConnection` pointing at `authdev-db`. Everything talks over the `db_net` bridge network services reach each other by name (`authdev-db`, `keycloak-db`).

## Keycloak Profile

`keycloak` and `keycloak-db` sit behind the `keycloak` compose profile: the base `up` starts AuthKit with its own database, while `docker compose --profile keycloak up` adds the identity stack. The realm file is mounted readonly and imported on boot (`--import-realm`) bootstrap admin is `admin` / `admin`.

After the first start, open http://localhost:8081 and confirm the `authz` realm exists. Client credentials come from `.env` (`KEYCLOAK_CLIENT_ID`, `KEYCLOAK_CLIENT_SECRET`).

## Environment

Copy of what `.env.example` declares set these before the first `up`:

| Variable | Meaning |
|----------|---------|
| `DEV_CERT_PATH` / `DEV_CERT_PASSWORD` | Dev HTTPS certificate and its password |
| `DEV_CERT_PORT_REST` / `DEV_CERT_PORT_GRPC` | Ports the certificate covers (`5000` / `5001`) |
| `KEYCLOAK_URL` / `KEYCLOAK_REALM` | Where the host finds Keycloak and which realm |
| `KEYCLOAK_CLIENT_ID` / `KEYCLOAK_CLIENT_SECRET` | Client credentials (secret is required) |
| `JWT__KEY` / `JWT__ISSUER` / `JWT__AUDIENCE` | Developer-token signing and validation |
| `ERRORMETADATA__DOCSBASEURL` | Base URL rendered into error metadata links |

At minimum, change `DEV_CERT_PASSWORD` and `KEYCLOAK_CLIENT_SECRET` the example values are public.

## Certificates

HTTPS terminates on a dev certificate from `./certs` (`DEV_CERT_PATH`, `DEV_CERT_PASSWORD` in `.env`). Mount your own `.pfx` there for a trusted local chain never commit the real password. `curl -k` in examples skips verification precisely because this is a dev cert.

:::warning[Never commit secrets]
`.env` holds passwords and keys. It is git-ignored for a reason leaked `DEV_CERT_PASSWORD` or `KEYCLOAK_CLIENT_SECRET` means rotating credentials everywhere.
:::

## Health & Logs

`auth` carries a healthcheck (`curl -k https://localhost:5000/health`, 30s interval, 3 retries) databases use `pg_isready` (5s interval). Useful commands:

```bash
docker compose ps            # anything not healthy/running fails fast
docker compose logs -f auth  # host output (Wolverine/Marten/Npgsql silenced to None by default)
docker compose logs -f authdev-db
```

If `auth` never becomes healthy, check the database first non healthy `authdev-db` blocks the host by design (`service_healthy` condition).

## Rebuilding & Cleanup

```bash
docker compose up --build        # rebuild after code changes
docker compose down              # stop, keep volumes (data stays)
docker compose down -v            # stop AND drop volumes (fresh database)
```

Use `down -v` when you want to replay storage migration from scratch or discard a broken dev database.

:::danger[down -v destroys data]
Dropping volumes wipes the encrypted keystore previously issued tokens stop verifying and must be reissued. There is no undo.
:::

## Troubleshooting

- **Port already in use** another Postgres on `5434`/`5433` or something on `5000`/`8081`: stop it or remap the left side of the `ports` entry.
- **Keycloak realm missing** the profile wasn't enabled (`--profile keycloak`) or the realm file wasn't mounted check `Deploy/Keycloak/realms/realm-authz.json` exists.
- **`auth` exits immediately** usually `.env` missing or `DEV_CERT_PASSWORD` wrong so Kestrel can't load the certificate read `docker compose logs auth`.
- **Token calls fail after restart** with fresh volume the keystore is new, so previously issued tokens no longer verify. Expected: rotate or reissue.

## Without Docker

The host is plain ASP.NET Core on Kestrel:

```bash
dotnet restore AuthKit.slnx
dotnet build AuthKit.slnx --configuration Release
dotnet run --project src/Host/Host.csproj
```

Default listen address: `http://0.0.0.0:8080` (HTTP/2). You still need PostgreSQL and Keycloak reachable see [Quick Start](/guide/quick-start/#option-2-run-locally).

## Toward Production

- Put real secrets in vault or the platform's secret store, never in `.env` files.
- Terminate TLS at the edge (reverse proxy / ingress) with a proper certificate instead of the dev `.pfx`.
- Back up the `AuthDev-data` volume it holds the encrypted keystore and token bindings.
- Pin image tags (`postgres:15` floats) and set explicit resource limits per service.
