# Configuration

AuthKit is configured through environment variables and `appsettings.json`.

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `KEYCLOAK_URL` | Keycloak base URL | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Keycloak realm | `authz` |
| `KEYCLOAK_CLIENT_ID` | Keycloak client ID | `workspace-authz` |
| `KEYCLOAK_CLIENT_SECRET` | Keycloak client secret | _required_ |
| `DEV_CERT_PATH` | Path to dev HTTPS certificate (pfx) | `/root/certs/devcert.pfx` |
| `DEV_CERT_PASSWORD` | Password for dev certificate | _empty_ |
| `DEV_CERT_PORT_REST` | Kestrel REST listener port | `5000` |
| `DEV_CERT_PORT_GRPC` | Kestrel gRPC listener port | `5001` |

## Key Configuration Sections

### Connection Strings

```json
{
  "ConnectionStrings": {
    "Marten": "Host=authdev-db;Port=5432;Database=AuthDev;Username=postgres;Password=postgres"
  }
}
```

### Encryption

```json
{
  "Encryption": {
    "AES_MASTER_KEY": "your-256-bit-base64-encoded-aes-master-key"
  }
}
```

### Server

```json
{
  "Server": {
    "Host": "http://0.0.0.0:8080",
    "Issuer": "https://authkit.local"
  }
}
```

### AuthKit Settings

Per-plugin scoped sections under `Plugins` (e.g. DevTokens):

```json
{
  "Plugins": {
    "authkit.devtokens": {
      "MaxDeveloperTokens": 3
    }
  },
  "AuthKit": {
    "PluginsPath": "./plugins"
  }
}
```

### Service Discovery

```json
{
  "ServiceDiscovery": {
    "EnableLogging": false,
    "AllowedNamespaces": [".Services", ".Repositories"],
    "ExcludedNamespaces": [".DTO", ".Entity"],
    "AllowedLayers": ["Core", "Host"],
    "SkipInterfaces": true,
    "SkipExceptions": true,
    "Lifetime": "Scoped"
  }
}
```

## Wolverine

Command/query handling, validation, and durability live on their own page: [Wolverine](/infrastructure/wolverine/).

## Marten

Document store, plugin schemas, and sessions live on their own page: [Marten + PostgreSQL](/infrastructure/marten/).

## Custom Configuration

Create an `appsettings.Production.json` for production settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "Plugins": {
    "authkit.devtokens": {
      "MaxDeveloperTokens": 10
    }
  }
}
```

:::note
The `appsettings.json` in the project root provides sensible defaults for development.
:::