# Konfiguracja

AuthKit konfiguruje się przez zmienne środowiskowe i `appsettings.json`.

## Zmienne środowiskowe

| Zmienna | Opis | Domyślnie |
|---------|------|-----------|
| `KEYCLOAK_URL` | Bazowy URL Keycloak | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Realm Keycloak | `authz` |
| `KEYCLOAK_CLIENT_ID` | ID klienta Keycloak | `workspace-authz` |
| `KEYCLOAK_CLIENT_SECRET` | Sekret klienta Keycloak | _wymagany_ |
| `DEV_CERT_PATH` | Ścieżka do deweloperskiego certyfikatu HTTPS (pfx) | `/root/certs/devcert.pfx` |
| `DEV_CERT_PASSWORD` | Hasło do certyfikatu deweloperskiego | _puste_ |
| `DEV_CERT_PORT_REST` | Port nasłuchu Kestrel REST | `5000` |
| `DEV_CERT_PORT_GRPC` | Port nasłuchu Kestrel gRPC | `5001` |

## Kluczowe sekcje konfiguracji

### Connection Strings

```json
{
  "ConnectionStrings": {
    "Marten": "Host=authdev-db;Port=5432;Database=AuthDev;Username=postgres;Password=postgres"
  }
}
```

### Szyfrowanie

```json
{
  "Encryption": {
    "AES_MASTER_KEY": "twój-256-bitowy-klucz-aes-zakodowany-base64"
  }
}
```

### Serwer

```json
{
  "Server": {
    "Host": "http://0.0.0.0:8080",
    "Issuer": "https://authkit.local"
  }
}
```

### Ustawienia AuthKit

Sekcje zakresowe per plugin pod `Plugins` (np. DevTokens):

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

Obsługa komend/zapytań, walidacja i trwałość żyją na osobnej stronie: [Wolverine](/pl/infrastructure/wolverine/).

## Marten

Magazyn dokumentów, schematy wtyczek i sesje żyją na osobnej stronie: [Marten + PostgreSQL](/pl/infrastructure/marten/).

## Własna konfiguracja

Utwórz `appsettings.Production.json` dla ustawień produkcyjnych:

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
`appsettings.json` w katalogu głównym projektu zawiera sensowne wartości domyślne do rozwoju.
:::
