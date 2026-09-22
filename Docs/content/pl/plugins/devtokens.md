# DevTokens

Wtyczka `DevTokens` (`authkit.devtokens`) wydaje, weryfikuje i autoryzuje tokeny deweloperskie do dostępu SDK. Klucze podpisujące i powiązania tokenów z kluczami pochodzą z Core hosta wtyczka jest właścicielem cyklu życia tokenów, nie infrastruktury kluczy.

## Cykl życia tokenu

1. **Utwórz** `POST /sdk/developer-tokens` z nazwą, zakresami i czasem życia. Zwraca podpisany JWT plus klucz `rk_live_…`.
2. **Utrwal** tokeny lądują w Marten z metadanymi (zakresy, wygaśnięcie).
3. **Zweryfikuj** `POST /sdk/tokens/verify` weryfikuje w miarę możliwości bez zapytań do bazy.
4. **Unieważnij i rotuj** `POST /sdk/tokens/{tokenId}/revoke-rotate` podmienia token jednym wywołaniem.

Pełne kształty endpointów: [REST API Reference](/pl/reference/rest-api/).

## Zakresy

Tokeny niosą zakresy (np. `write:packages`). Żądania są autoryzowane względem zakresów z momentu wydania token nigdy nie rośnie ponad nie.

## Używanie tokenów

Wywołania SDK przedstawiają dwa poświadczenia:

```http
Authorization: Bearer <keycloak-token>
X-Developer-Token: <authkit-jwt>
```

Potok weryfikuje oba i egzekwuje dostęp do zakresów. Zobacz [Używanie tokenów w żądaniach SDK](/pl/reference/rest-api/#Używanie-tokenów-w-żądaniach-SDK).

## Walidacja

Modele żądań są walidowane przez FluentValidation zanim cokolwiek dotknie magazynu błędne payloady odpadają szybko ze szczegółami problemu RFC 7807.
