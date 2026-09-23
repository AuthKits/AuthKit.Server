<div align="center">

# AuthKit

### Developer Authentication & SDK Access Service

[![License](https://img.shields.io/badge/license-MIT%20%2B%20Commons%20Clause-7c3aed?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)

</div>

---

## Overview

AuthKit is **plugin based service** for handling **developer authentication, SDK token issuance, and access verification**. It ensures that only authorized developers can access SDK methods and provides secure, auditable token-based authentication mechanism.

AuthKit validates user identity through **Keycloak**, issues signed developer tokens (JWT), and exposes both **RESTful** and **gRPC** endpoints so SDK clients can request, verify, and manage tokens.

## Features

- **Secure Access** - Issue and validate developer tokens for SDK usage
- **Role & Scope Enforcement** - Restrict SDK methods to authorized roles and token scopes
- **Key Management** - RSA signing-key generation, AES-encrypted key storage, JWKS discovery, and key rotation
- **Plugin Architecture** - Functionality is delivered through dynamically discovered plugins (`IAuthKitPlugin`)
- **Extensible** - Add new token types, policies, or SDK solutions without modifying the host
- **Dual Transport** - Same services exposed over REST (HTTP/2) and gRPC

## Architecture

AuthKit is composed of three layers:

- **Core** - Shared domain, JWT signing-key management (RSA key generation, AES encryption, on-disk keystore), token key bindings, and core options.
- **Host** - ASP.NET Core host running on Kestrel. Wires up **Wolverine** - (command/query handling), **Marten** (PostgreSQL event/document store), RESTful and gRPC endpoints, Keycloak integration, CLI, and dynamic plugin loading.
- **Plugins** - Extensions discovered and loaded dynamically from the `plugins/` directory at startup. A plugin contributes services, middleware, health checks, and OpenAPI security schemes through the `IAuthKitPlugin` contract without being referenced by the host.

```
src/
├── Core/                 # Domain, key management, options
├── Host/                 # Web host, REST/gRPC, CLI, plugin loader
│   ├── Configuration/    # Auth, Keycloak, Kestrel, Marten, ServiceDiscovery
│   ├── Grpc/             # gRPC services and protos
│   ├── KeyManagement/    # JWKS endpoint, key store initializer
│   ├── Restful/          # Host-level middleware
│   └── ServiceDiscovery/ # Automatic DI registration
└── Plugins/
    ├── Abstractions/     # IAuthKitPlugin contract
    └── Solutions/        # Plugin implementations (e.g. DevTokens)
```

## Plugin Model

Plugins implement `IAuthKitPlugin` and are loaded from the directory configured by `AuthKit:PluginsPath` (defaults to `<base>/plugins`). At startup the host:

1. Discovers and loads plugin assemblies.
2. Calls `ConfigureServices` to register each plugin's dependencies.
3. Inserts any contributed `MiddlewareType` into the pipeline.
4. Exposes contributed OpenAPI security schemes.
5. Reports plugin health through `CheckHealthAsync`.

This lets new SDK solutions ship as self-contained packages without changing the host project.

## Built-in Plugins

### DevTokens

Issues and validates developer tokens used for SDK access. Exposed REST endpoints:

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `sdk/developer-tokens` | Create a developer token |
| `GET` | `sdk/developer-tokens` | List developer tokens |
| `GET` | `sdk/developer-tokens/{tokenId}` | Get a token by id |
| `DELETE` | `sdk/developer-tokens/{tokenId}` | Delete a token |
| `POST` | `sdk/tokens/verify` | Verify a developer token |
| `POST` | `sdk/tokens/{tokenId}/revoke-rotate` | Revoke and rotate a token |

Tokens are passed via the `X-Developer-Token` API-key header and enforced by the `DeveloperTokenMiddleware` and scope-based authorization (`DeveloperScopeRequirement`).

## JWT & Key Management

- Signing keys are generated as RSA keys, encrypted with the AES master key (`Encryption:AES_MASTER_KEY`), and persisted in the key store.
- Public keys are published at `.well-known/jwks.json` for external signature verification.
- Multiple active keys are supported simultaneously to allow seamless key rotation.
- Token key bindings associate issued tokens with the signing key used to protect them.

## Authentication

User identity is validated with **Keycloak** JWT bearer authentication. Keycloak is configured through environment variables:

| Variable | Description | Default |
| --- | --- | --- |
| `KEYCLOAK_URL` | Keycloak base URL | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Keycloak realm | `authz` |
| `KEYCLOAK_CLIENT_ID` | Keycloak client id | `workspace-authz` |

Client roles from the `resource_access` claim are mapped to ASP.NET Core role claims for authorization.

## Configuration

Key `appsettings.json` sections:

| Section | Purpose |
| --- | --- |
| `ConnectionStrings:Marten` | PostgreSQL connection for Marten |
| `AuthKit:MaxDeveloperTokens` | Max tokens per developer (default `3`) |
| `Encryption:AES_MASTER_KEY` | Master key for encrypting signing keys |
| `Server:Issuer` | JWT issuer (`https://authkit.local`) |
| `ServiceDiscovery` | Automatic DI registration rules (namespaces, layers, lifetime) |

## Getting Started

### Run with Docker

```bash
docker compose up --build
```

This starts AuthKit (REST on `5000`, gRPC on `5001`), a PostgreSQL database, and Keycloak. Configuration is provided via `.env` (see `.env.example`).

### Run locally

```bash
dotnet build AuthKit.slnx
dotnet run --project src/Host/Host.csproj
```

The host listens on the address configured in `Server:Host` (default `http://0.0.0.0:8080`, HTTP/2).

## Documentation

| Topic | Link |
| --- | --- |
| Documentation index | [Docs](Docs/README.md) |
| SveltePress site | Run `task docs:dev` locally |
| Schemas & Diagrams | [Schemas](Docs/Schemas.md) |
| Architecture Decision Records | [ADRs](Docs/content/en/adr/README.md) |

## Contributing

We welcome contributions that improve contract clarity, expand integration patterns, or enhance type safety.

Please read the [Contributing Guide](CONTRIBUTING.md) for setup, workflow, and pull request guidelines.

## License

**MIT License + Commons Clause**

AuthKit is open source for personal, educational, and research purposes. Commercial use requires explicit permission.

### Permitted Use ✓

- Personal projects and learning
- Academic research and education
- Open source contributions
- Non-commercial experimentation

### Restricted Use ✗

- Commercial products and services
- SaaS platform offerings
- Software resale or licensing
- Consulting services without approval

See [LICENSE](LICENSE) for complete terms.

## Code of Conduct

Please read and follow the [Code of Conduct](CODE_OF_CONDUCT.md) when participating in this project.

## Security

For vulnerability reporting, see the [Security Policy](SECURITY.md).
