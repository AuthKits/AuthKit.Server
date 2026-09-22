# Introduction

AuthKit is a **plugin-based service** for handling **developer authentication, SDK token issuance, and access verification**.

It ensures that only authorized developers can access SDK methods and provides a secure, auditable token-based authentication mechanism.

<div class="intro-cta">
<a class="intro-cta__primary" href="/en/guide/quick-start/">Get Started</a>
<a class="intro-cta__secondary" href="/en/reference/rest-api/">REST API Reference</a>
</div>

<script>
  import IntroCards from '$lib/IntroCards.svelte'

  const howCards = [
    { icon: 'shield', title: 'Authenticate', desc: 'Developers sign in through Keycloak, the external JWT authority.' },
    { icon: 'key', title: 'Issue', desc: 'AuthKit issues signed developer tokens (JWT) backed by the Core keystore.' },
    { icon: 'sync', title: 'Consume', desc: 'SDK clients request, verify, and manage tokens over RESTful and gRPC endpoints.' },
  ]
  const transportCards = [
    { icon: 'code', title: 'REST API', desc: 'Developer tokens over HTTPS with RFC 7807 problem details.', href: '/en/reference/rest-api/' },
    { icon: 'sync', title: 'gRPC', desc: 'High-performance contract-first endpoints for backend-to-backend calls.', href: '/en/reference/grpc/' },
  ]
  const infraCards = [
    { icon: 'shield', title: 'Keycloak', desc: 'External JWT authority for developer sign-in.', href: '/en/infrastructure/keycloak/' },
    { icon: 'db', title: 'Marten + PostgreSQL', desc: 'Encrypted keystore and token bindings as persisted documents.', href: '/en/infrastructure/marten/' },
    { icon: 'layers', title: 'Wolverine', desc: 'Command and query handling inside the host pipeline.', href: '/en/infrastructure/wolverine/' },
    { icon: 'box', title: 'Docker + Kestrel', desc: 'Run the whole stack with one compose file.', href: '/en/infrastructure/docker/' },
  ]
  const pluginCards = [
    { icon: 'key', title: 'DevTokens', desc: 'Issues and validates developer tokens for SDK access.', href: '/en/plugins/devtokens/' },
    { icon: 'terminal', title: 'DevTools', desc: 'Hosted tooling: Swagger via reflection and developer utilities.', href: '/en/plugins/devtools/' },
    { icon: 'plug', title: 'Custom plugins', desc: 'Drop your own solution behind the IAuthKitPlugin contract.', href: '/en/plugins/custom/' },
  ]
</script>

## How AuthKit Works

<IntroCards cards={howCards} />

## Transports

Pick how SDK clients talk to AuthKit.

<IntroCards cards={transportCards} columns={2} />

## Infrastructure

AuthKit stands on proven building blocks, composed by the Host.

<IntroCards cards={infraCards} columns={2} />

## Plugins & Tools

Extend behavior or inspect the running server.

<IntroCards cards={pluginCards} />

## Quick Start

1. Start the AuthKit service with Docker
2. Configure Keycloak
3. Create developer tokens for SDK access
4. Use tokens to authenticate SDK requests

:::tip[Recommended path]
Continue to the [Quick Start](/guide/quick-start/) guide to get running in minutes.
:::

## Architecture

AuthKit is composed of three layers:

### Core

Shared domain, JWT signing-key management (RSA key generation, AES encryption, on-disk keystore), token key bindings, and core options.

### Host

ASP.NET Core host running on Kestrel. Provides Wolverine (command/query handling), Marten (PostgreSQL event/document store), RESTful and gRPC endpoints, Keycloak integration, CLI, and dynamic plugin loading.

### Plugins

Extensions discovered and loaded dynamically from the `plugins/` directory. Plugins contribute services, middleware, health checks, and OpenAPI security schemes through the `IAuthKitPlugin` contract.

```text
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

## Built-in Plugins

### DevTokens

Issues and validates developer tokens for SDK access.

## REST API Reference

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `sdk/developer-tokens` | Create a developer token |
| `GET` | `sdk/developer-tokens` | List developer tokens |
| `GET` | `sdk/developer-tokens/{tokenId}` | Get a token by ID |
| `DELETE` | `sdk/developer-tokens/{tokenId}` | Delete a token |
| `POST` | `sdk/tokens/verify` | Verify a developer token |
| `POST` | `sdk/tokens/{tokenId}/revoke-rotate` | Revoke and rotate a token |

## Next Steps

1. [Quick Start](/guide/quick-start/)
2. [REST API Reference](/reference/rest-api/)
3. [Configuration](/guide/configuration/)
