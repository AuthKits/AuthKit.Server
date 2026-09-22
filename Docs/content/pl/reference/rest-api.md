# Dokumentacja REST API

AuthKit udostępnia endpointy REST i gRPC do zarządzania tokenami i ich weryfikacji.

## Uwierzytelnianie

Wszystkie endpointy API wymagają uwierzytelnienia przez **token JWT Keycloak (bearer)** w nagłówku `Authorization`.

```http
Authorization: Bearer <keycloak-access-token>
```

## API tokenów deweloperskich

### Tworzenie tokenu deweloperskiego

Tworzy nowy token deweloperski dla uwierzytelnionego użytkownika.

**Endpoint:** `POST /sdk/developer-tokens`

**Nagłówki:**
| Nagłówek | Wartość |
|----------|---------|
| `Authorization` | `Bearer <keycloak-token>` |
| `Content-Type` | `application/json` |

**Ciało żądania:**
```json
{
  "name": "Mój token SDK",
  "description": "Token do dostępu do Marketplace API",
  "scopes": ["read:products", "write:products"],
  "lifetimeDays": 7
}
```

**Odpowiedź (200):**
```json
{
  "jwt": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjEyMzQ1Njc4OTAifQ...",
  "key": "rk_live_xxx",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "developerId": "550e8400-e29b-41d4-a716-446655440001",
  "scopes": ["read:products", "write:products"],
  "lifetime": {
    "createdAt": "2024-01-15T10:30:00Z",
    "expiresAt": "2024-01-22T10:30:00Z"
  }
}
```

---

### Lista tokenów deweloperskich

Zwraca wszystkie tokeny deweloperskie należące do uwierzytelnionego użytkownika.

**Endpoint:** `GET /sdk/developer-tokens`

**Nagłówki:**
| Nagłówek | Wartość |
|----------|---------|
| `Authorization` | `Bearer <keycloak-token>` |

**Odpowiedź (200):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "developerId": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Produkcyjny token SDK",
    "scopes": ["read:products"],
    "createdAt": "2024-01-10T08:00:00Z",
    "expiresAt": "2024-02-10T08:00:00Z",
    "isExpired": false
  }
]
```

---

### Pobieranie tokenu po ID

Pobiera konkretny token deweloperski po jego ID.

**Endpoint:** `GET /sdk/developer-tokens/{tokenId}`

**Nagłówki:**
| Nagłówek | Wartość |
|----------|---------|
| `Authorization` | `Bearer <keycloak-token>` |

**Odpowiedź (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "developerId": "550e8400-e29b-41d4-a716-446655440001",
  "name": "Mój token SDK",
  "scopes": ["read:products", "write:products"],
  "createdAt": "2024-01-15T10:30:00Z",
  "expiresAt": "2024-01-22T10:30:00Z",
  "isExpired": false
}
```

**Błąd (404):** Nie znaleziono tokenu

---

### Usuwanie tokenu deweloperskiego

Usuwa token deweloperski po jego ID.

**Endpoint:** `DELETE /sdk/developer-tokens/{tokenId}`

**Nagłówki:**
| Nagłówek | Wartość |
|----------|---------|
| `Authorization` | `Bearer <keycloak-token>` |

**Odpowiedź (200):**
```
Token usunięty pomyślnie
```

---

## API weryfikacji tokenów

### Weryfikacja tokenu

Weryfikuje token deweloperski względem utrwalonych danych tokenu. Endpoint anonimowy (nagłówek `Authorization` nie jest wymagany).

**Endpoint:** `POST /sdk/tokens/verify`

**Ciało żądania:**
```json
{
  "key": "rk_live_xxx"
}
```

**Odpowiedź (200):**
```json
true
```

---

### Unieważnienie i rotacja tokenu

Unieważnia istniejący token i generuje zamiennik. Wymaga roli `User` (`Authorization: Bearer <keycloak-token>` z rolą `User`).

**Endpoint:** `POST /sdk/tokens/{tokenId}/revoke-rotate`

**Nagłówki:**
| Nagłówek | Wartość |
|----------|---------|
| `Authorization` | `Bearer <keycloak-token>` |

**Odpowiedź (200):**
```json
{
  "jwt": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjY4ODkwMTIzNDU2fQ...",
  "key": "rk_live_yyy",
  "tokenId": "660e8400-e29b-41d4-a716-446655440000",
  "developerId": "550e8400-e29b-41d4-a716-446655440001",
  "scopes": ["read:products", "write:products"],
  "lifetime": {
    "createdAt": "2024-01-16T10:30:00Z",
    "expiresAt": "2024-01-23T10:30:00Z"
  }
}
```

## Odpowiedzi błędów

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Brak lub nieprawidłowe dane uwierzytelniające."
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Żądany zasób nie został znaleziony."
}
```

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Brak uprawnień do tego zasobu."
}
```

## Używanie tokenów w żądaniach SDK

Tokeny deweloperskie przekazywane są w nagłówku `X-Developer-Token`:

```http
GET /api/products HTTP/1.1
Host: api.example.com
Authorization: Bearer <keycloak-token>
X-Developer-Token: <authkit-jwt>
```

API zweryfikuje oba tokeny i upewni się, że deweloper ma dostęp do żądanych zakresów (scopes).
