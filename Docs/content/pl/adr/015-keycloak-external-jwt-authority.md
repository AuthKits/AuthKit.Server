[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/014-error-responses-via-middleware/) | [Następny](/pl/adr/016-marten-and-wolverine-infrastructure/)

# [ADR-015] Używanie Keycloak jako zewnętrznego urzędu JWT

*2026-08* | Status: accepted

**Tag:** #adr_015

**Date:** 2026-08-26

**Scope:** Host.Configuration

## Kontekst

Host musi uwierzytelniać wywołujących (w tym administracyjne i chronione przez wtyczki punkty końcowe) przy użyciu podpisanych JWT wystawianych przez prawdziwego dostawcę tożsamości, zamiast samodzielnie wystawiać lub weryfikować tokeny.

## Problem

Budowanie lokalnego magazynu użytkowników lub tokenów wystawianych samodzielnie wiąże hosta z logiką tożsamości, której nie powinien posiadać, a klienci oczekują standardowych przepływów OIDC/JWT bearer. Host musi weryfikować tokeny i udostępniać role Keycloak autoryzacji ASP.NET Core bez ręcznego przetwarzania oświadczeń.

## Decyzja

`AddKeycloakServices` rejestruje uwierzytelnianie JWT bearer ASP.NET Core względem realmu Keycloak: `Authority` i `Audience` pochodzą z `KEYCLOAK_URL`/`KEYCLOAK_REALM`/`KEYCLOAK_CLIENT_ID` (z lokalnymi domyślnymi wartościami deweloperskimi), walidacja wystawcy i odbiorców jest włączona, `NameClaimType` to `preferred_username`, a hak `OnTokenValidated` mapuje role klienta Keycloak z `resource_access` na standardowe oświadczenia `ClaimTypes.Role`.

### Uzasadnienie projektowe

- Delegowanie do Keycloak utrzymuje hosta z dala od wystawiania tożsamości i dowodzi tokenów poprzez standardową walidację JWT bearer.
- Mapowanie ról `resource_access` na oświadczenia ról pozwala zwykłym politykom ASP.NET Core autoryzować bez kodu specyficznego dla Keycloak w procedurach obsługi.
- Konfiguracja sterowana środowiskiem wspiera realms deweloperskie, testowe i produkcyjne z tego samego kodu.

## Odrzucone

- Samodzielnie wystawiana lub lokalna usługa tokenów wewnątrz hosta.
- Niestandardowa baza użytkowników/ról.
- Wyłączanie walidacji wystawcy/odbiorców.

## Konsekwencje

Wywołujący uwierzytelniają się standardowymi JWT wystawianymi przez Keycloak, a polityki oparte na rolach działają jednolicie. Dwa celowe ustępstwa deweloperskie są udokumentowane jako kompromisy: `RequireHttpsMetadata = false` oraz procedura obsługi kanału zwrotnego `DangerousAcceptAnyServerCertificateValidator` oba muszą zostać zaostrzone przed jakimkolwiek wdrożeniem produkcyjnym.

## Powiązane

- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - chroni obie powierzchnie transportowe
- [ADR-014](/pl/adr/014-error-responses-via-middleware/) - nieautoryzowane/zabronione renderowane tutaj

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/014-error-responses-via-middleware/) | [Następny](/pl/adr/016-marten-and-wolverine-infrastructure/)
