# REST API Reference

AuthKit exposes both REST and gRPC endpoints for token management and verification.

## Authentication

All API endpoints require authentication via **Keycloak JWT bearer token** in the `Authorization` header.

```http
Authorization: Bearer <keycloak-access-token>
```

## Developer Tokens API

### Create Developer Token

Creates a new developer token for the authenticated user.

**Endpoint:** `POST /sdk/developer-tokens`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer <keycloak-token>` |
| `Content-Type` | `application/json` |

**Request Body:**
```json
{
  "name": "My SDK Token",
  "description": "Token for accessing marketplace API",
  "scopes": ["read:products", "write:products"],
  "lifetimeDays": 7
}
```

**Response (200):**
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

### List Developer Tokens

Lists all developer tokens belonging to the authenticated user.

**Endpoint:** `GET /sdk/developer-tokens`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer <keycloak-token>` |

**Response (200):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "developerId": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Production SDK Token",
    "scopes": ["read:products"],
    "createdAt": "2024-01-10T08:00:00Z",
    "expiresAt": "2024-02-10T08:00:00Z",
    "isExpired": false
  }
]
```

---

### Get Developer Token by ID

Retrieves a specific developer token by its ID.

**Endpoint:** `GET /sdk/developer-tokens/{tokenId}`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer <keycloak-token>` |

**Response (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "developerId": "550e8400-e29b-41d4-a716-446655440001",
  "name": "My SDK Token",
  "scopes": ["read:products", "write:products"],
  "createdAt": "2024-01-15T10:30:00Z",
  "expiresAt": "2024-01-22T10:30:00Z",
  "isExpired": false
}
```

**Error (404):** Token not found

---

### Delete Developer Token

Deletes a developer token by its ID.

**Endpoint:** `DELETE /sdk/developer-tokens/{tokenId}`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer <keycloak-token>` |

**Response (200):**
```
Token deleted successfully
```

---

## Token Verification API

### Verify Token

Verifies a developer token against its persisted token data. This endpoint is anonymous (no `Authorization` header required).

**Endpoint:** `POST /sdk/tokens/verify`

**Request Body:**
```json
{
  "key": "rk_live_xxx"
}
```

**Response (200):**
```json
true
```

---

### Revoke and Rotate Token

Revokes an existing token and generates a replacement. Requires the `User` role (`Authorization: Bearer <keycloak-token>` with `User` role claim).

**Endpoint:** `POST /sdk/tokens/{tokenId}/revoke-rotate`

**Headers:**
| Header | Value |
|--------|-------|
| `Authorization` | `Bearer <keycloak-token>` |

**Response (200):**
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

## Error Responses

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication credentials are missing or invalid."
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found."
}
```

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to access this resource."
}
```

## Using Tokens in SDK Requests

Developer tokens are passed via the `X-Developer-Token` header:

```http
GET /api/products HTTP/1.1
Host: api.example.com
Authorization: Bearer <keycloak-token>
X-Developer-Token: <authkit-jwt>
```

The API will validate both tokens and ensure the developer has access to the requested scopes.