# gRPC Reference

AuthKit exposes a gRPC surface next to REST from the same host pipeline (see [ADR-013](/adr/013-dual-rest-and-grpc-transport/)). Use it for backend-to-backend calls where contract-first protobuf and streaming-friendly transport pay off.

Proto sources live in `src/Host/Grpc/protos/` (`jwks.proto`, `monitoring.proto`); plugins can contribute their own services the same way.

## Services

| Service | Package | Description |
|---------|---------|-------------|
| [`Jwks`](#Jwks-Service) | `jwks` | Public signing keys, single-key lookup, health and stats |
| [`Monitoring`](#Monitoring-Service) | `monitoring` | Aggregate host health and process metrics |

## Jwks Service

Public signing keys in JWKS shape — the same data the REST layer serves, projected through protobuf.

```proto
service Jwks {
  rpc GetJwks (google.protobuf.Empty) returns (JwksResponse);
  rpc GetKeyByKid (KeyLookupRequest) returns (SingleKeyResponse);
  rpc GetJwksHealth (google.protobuf.Empty) returns (HealthResponse);
  rpc GetJwksStats (google.protobuf.Empty) returns (KeyStoreStatsResponse);
}
```

### GetJwks

Returns all active (non-revoked) public signing keys.

**Request:** `google.protobuf.Empty`

**Response:** `JwksResponse` with repeated `Jwk` (`kty`, `use`, `kid`, `alg`, `n`, `e`, `x5c`).

### GetKeyByKid

Returns a single key by its key ID together with lifecycle metadata.

**Request:**

```proto
message KeyLookupRequest {
  string kid = 1;
}
```

**Response:** `SingleKeyResponse` (`Jwk key`, `KeyMetadata metadata` with `kid`, `created_at`, `revoked`, `algorithm`, `purpose`).

### GetJwksHealth

Health status of the JWKS key store: `status`, `available_keys`, `active_key_id`, `details` map, `timestamp`.

### GetJwksStats

Key store statistics for monitoring: `total_keys`, oldest/newest key age, `key_ids`, `timestamp`.

## Monitoring Service

```proto
service Monitoring {
  rpc GetHealth (google.protobuf.Empty) returns (MonitoringHealthResponse);
  rpc GetMetrics (google.protobuf.Empty) returns (MetricsResponse);
}
```

### GetHealth

Aggregate health of the host and its plugins: `status`, `time`, `jwt_key_store_healthy`, repeated `PluginHealth` (name + check results with status, reason, tags, data).

### GetMetrics

Process metrics: `uptime_seconds`, `process_start_unix_time`, `time`.

## Calling with grpcurl

```bash
grpcurl --insecure localhost:5001 list
grpcurl --insecure localhost:5001 jwks.Jwks/GetJwks
grpcurl --insecure -d '{"kid": "<key-id>"}' localhost:5001 jwks.Jwks/GetKeyByKid
```

Replace `localhost:5001` with your Kestrel endpoint from [Configuration](/guide/configuration/). Kestrel serves HTTPS by default when a dev certificate exists (`DEV_CERT_PATH`); use `--insecure` for the dev cert, or plain `-plaintext` only when no certificate is configured (HTTP fallback).

## Authentication

gRPC calls go through the same global interceptors as the rest of the pipeline. Pass the Keycloak JWT the same way the REST surface expects it (bearer token) — see [Authentication](/reference/rest-api/#authentication).

## Plugin Services

Plugins can expose their own gRPC services next to the host ones (eg. the example `authkit.example.ExampleGreeter` with `SayHello`). They are discovered and mapped together with host endpoints — see [ADR-009](/adr/009-dynamic-plugin-discovery/).
