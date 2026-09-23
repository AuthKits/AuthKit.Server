# Keycloak

Keycloak to zewnętrzny urząd JWT, przez który logują się deweloperzy. AuthKit nigdy nie przechowuje haseł deweloperów weryfikuje tokeny dostępowe wydane przez Keycloak i na ich podstawie bije własne tokeny SDK. Zobacz [ADR-015](/pl/adr/015-keycloak-external-jwt-authority/).

## Jak to działa

Przepływ ma trzy kroki i jasny podział odpowiedzialności:

1. **Logowanie** deweloper uwierzytelnia się w Keycloak standardowym przepływem OIDC i dostaje JWT (bearer).
2. **Weryfikacja** host waliduje token przez ASP.NET Core JWT bearer względem realm (`AddKeycloakServices`): issuer i audience muszą się zgadzać, `NameClaimType` to `preferred_username`.
3. **Role** hook `OnTokenValidated` mapuje role klienta z `resource_access` na standardowe `ClaimTypes.Role`, więc zwykłe polityki autoryzacji działają bez kodu specyficznego dla Keycloak w handlerach.

Odrzucone alternatywy: własny lokalny magazyn użytkowników, tokeny self-issued w hoście, własna baza ról wszystko to wiązałoby host z logiką tożsamości, której nie powinien posiadać.

## Konfiguracja

1. Otwórz http://localhost:8081, zaloguj `admin` / `admin`.
2. Zaimportuj realm z `Deploy/Keycloak/realms/realm-authz.json` (w compose montowany read-only z auto-importem).
3. Ustaw sekret klienta w `.env` (`KEYCLOAK_CLIENT_SECRET`).

## Zmienne środowiskowe

| Zmienna | Znaczenie | Domyślnie |
|---------|-----------|-----------|
| `KEYCLOAK_URL` | Bazowy URL Keycloak (`Authority`) | `http://keycloak:8080` |
| `KEYCLOAK_REALM` | Realm Keycloak | `authz` |
| `KEYCLOAK_CLIENT_ID` | ID klienta Keycloak (`Audience`) | `workspace-authz` |
| `KEYCLOAK_CLIENT_SECRET` | Sekret klienta Keycloak | _wymagane_ |

Ta sama konfiguracja niesie dev, test i produkcję zmieniają się wartości, nie kod.

## Używanie tokenu

Każde wywołanie API niesie token Keycloak w nagłówku:

```http
Authorization: Bearer <keycloak-access-token>
```

Na jego podstawie AuthKit wydaje własne tokeny deweloperskie (`X-Developer-Token`) szczegóły w [REST API Reference](/pl/reference/rest-api/).

## Dev a produkcja

Świadome ustępstwa deweloperskie, do zaciśnięcia przed produkcją:

- `RequireHttpsMetadata = false`;
- backchannel z `DangerousAcceptAnyServerCertificateValidator`.

:::danger[Przed produkcją]
Oba powyższe muszą zostać zaciśnięte przed jakimkolwiek produkcyjnym deploymentem inaczej tokeny lecą po HTTP i walidacja certyfikatów nie istnieje.
:::

## Rozwiązywanie problemów

- **401 mimo poprawnego tokenu** sprawdź `KEYCLOAK_URL`/`KEYCLOAK_REALM`/`KEYCLOAK_CLIENT_ID`: issuer albo audience nie zgadza się z wystawcą tokenu.
- **Brak ról w claimach** role muszą siedzieć w `resource_access` klienta, nie na poziomie realm hook mapuje tylko role klienta.
- **Brak realm po starcie** profil `keycloak` nie włączony albo plik realm nie zamontowany zobacz [Docker + Kestrel](/pl/infrastructure/docker/).

Pełna referencja ustawień: [Konfiguracja](/pl/guide/configuration/).
