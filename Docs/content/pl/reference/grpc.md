# Dokumentacja gRPC

AuthKit udostępnia powierzchnię gRPC obok REST z tego samego potoku hosta (zobacz [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/)). Używaj jej do komunikacji backend-backend, gdzie opłacają się kontraktowe protobufy.

Źródła proto leżą w `src/Host/Grpc/protos/` (`jwks.proto`, `monitoring.proto`); wtyczki mogą dokładać własne serwisy tak samo.

## Serwisy

| Serwis | Pakiet | Opis |
|--------|--------|------|
| [`Jwks`](#Serwis-Jwks) | `jwks` | Publiczne klucze podpisujące, lookup po kid, health i statystyki |
| [`Monitoring`](#Serwis-Monitoring) | `monitoring` | Zbiorczy health hosta i metryki procesu |

## Serwis Jwks

Publiczne klucze podpisujące w kształcie JWKS — te same dane, które serwuje warstwa REST, rzutowane przez protobuf.

```proto
service Jwks {
  rpc GetJwks (google.protobuf.Empty) returns (JwksResponse);
  rpc GetKeyByKid (KeyLookupRequest) returns (SingleKeyResponse);
  rpc GetJwksHealth (google.protobuf.Empty) returns (HealthResponse);
  rpc GetJwksStats (google.protobuf.Empty) returns (KeyStoreStatsResponse);
}
```

### GetJwks

Zwraca wszystkie aktywne (nieunieważnione) publiczne klucze podpisujące.

**Żądanie:** `google.protobuf.Empty`

**Odpowiedź:** `JwksResponse` z powtarzanym `Jwk` (`kty`, `use`, `kid`, `alg`, `n`, `e`, `x5c`).

### GetKeyByKid

Zwraca pojedynczy klucz po identyfikatorze wraz z metadanymi cyklu życia.

**Żądanie:**

```proto
message KeyLookupRequest {
  string kid = 1;
}
```

**Odpowiedź:** `SingleKeyResponse` (`Jwk key`, `KeyMetadata metadata` z `kid`, `created_at`, `revoked`, `algorithm`, `purpose`).

### GetJwksHealth

Stan magazynu kluczy JWKS: `status`, `available_keys`, `active_key_id`, mapa `details`, `timestamp`.

### GetJwksStats

Statystyki magazynu do monitoringu: `total_keys`, wiek najstarszego/najnowszego klucza, `key_ids`, `timestamp`.

## Serwis Monitoring

```proto
service Monitoring {
  rpc GetHealth (google.protobuf.Empty) returns (MonitoringHealthResponse);
  rpc GetMetrics (google.protobuf.Empty) returns (MetricsResponse);
}
```

### GetHealth

Zbiorczy health hosta i wtyczek: `status`, `time`, `jwt_key_store_healthy`, powtarzane `PluginHealth` (nazwa + wyniki checków ze statusem, powodem, tagami, danymi).

### GetMetrics

Metryki procesu: `uptime_seconds`, `process_start_unix_time`, `time`.

## Wywołania przez grpcurl

```bash
grpcurl --insecure localhost:5001 list
grpcurl --insecure localhost:5001 jwks.Jwks/GetJwks
grpcurl --insecure -d '{"kid": "<key-id>"}' localhost:5001 jwks.Jwks/GetKeyByKid
```

Podmień `localhost:5001` na swój endpoint Kestrela z [Konfiguracji](/pl/guide/configuration/). Kestrel domyślnie serwuje HTTPS, gdy istnieje certyfikat deweloperski (`DEV_CERT_PATH`); użyj `--insecure` dla certyfikatu dev, a `-plaintext` tylko gdy brak certyfikatu (fallback HTTP).

## Uwierzytelnianie

Wywołania gRPC przechodzą przez te same globalne interceptory co reszta potoku. Przekazuj JWT Keycloak tak samo, jak oczekuje powierzchnia REST (bearer token) — zobacz [Uwierzytelnianie](/pl/reference/rest-api/#uwierzytelnianie).

## Serwisy wtyczek

Wtyczki mogą wystawiać własne serwisy gRPC obok hostowych (np. przykładowy `authkit.example.ExampleGreeter` z `SayHello`). Są odkrywane i mapowane razem z endpointami hosta — zobacz [ADR-009](/pl/adr/009-dynamic-plugin-discovery/).
