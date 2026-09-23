[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/004-token-key-bindings-domain/) | [Następny](/pl/adr/006-kid-as-generated-guid/)

# [ADR-005] Publikuj klucze publiczne poprzez JWKS, ujawniając wyłącznie klucze nieunieważnione

*2026-08* | Status: accepted

**Tag:** #adr_005

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Kontekst

Strony ufające weryfikują podpisy JWT, pobierając klucze publiczne wystawcy. Magazyn kluczy przechowuje już moduł RSA i wykładnik każdego klucza wraz z metadanymi cyklu życia oraz utrzymuje widok w pamięci wszystkich wczytanych kluczy.

## Problem

Konsumenci potrzebują standardowego, przyjaznego dla cachowania sposobu odkrywania ważnych kluczy publicznych. Jeśli publikowane są klucze unieważnione lub wygasłe albo odpowiedź jest przeliczana przy każdym żądaniu bez unieważniania, weryfikatory mogą akceptować złe podpisy lub punkt końcowy marnuje CPU na odtwarzanie zestawu kluczy.

## Decyzja

Magazyn kluczy udostępnia klucze publiczne poprzez `GetPublicJwks()` jako instancje `PublicJwkDto` w kształcie zgodnym z JWKS (`kty=RSA`, `use=sig`, `kid`, `alg`, `n`, `e`). Dołączane są wyłącznie klucze, których metadane `Revoked` mają wartość false, a wynik jest cachowany i przebudowywany tylko przy zmianie zestawu kluczy (rotacja lub unieważnienie).

### PublicJwkDto

**Obowiązki:**

- Przenosi pola JWKS potrzebne zewnętrznym weryfikatorom.
- Pozostaje projekcją tylko do odczytu wpisu klucza w pamięci.

### Uzasadnienie projektowe

- JWKS jest branżowym standardowym formatem odkrywania, więc gotowe weryfikatory integrują się bez własnego kodu.
- Wykluczanie unieważnionych kluczy zapobiega akceptowaniu podpisów z wycofanych kluczy.
- Cachowanie z jawnym unieważnianiem równoważy wydajność ze świeżością po rotacji.

## Odrzucone

- Publikowanie unieważnionych lub wygasłych kluczy w odpowiedzi JWKS.
- Regenerowanie zestawu kluczy przy każdym żądaniu bez cache.
- Wymyślanie własnego formatu odkrywania kluczy publicznych zamiast JWKS.

## Konsekwencje

Zewnętrzni weryfikatorzy otrzymują poprawny, standardowy zestaw kluczy, a punkt końcowy pozostaje wydajny. Kosztem jest jawne unieważnianie cache przy każdej zmianie cyklu życia oraz utrzymywanie projekcji w synchronizacji z metadanymi kluczy.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - magazyn kluczy udostępniający JWKS
- [ADR-003](/pl/adr/003-signing-key-lifecycle-immutable-transitions/) - cykl życia steruje filtrowaniem unieważnień
- [ADR-006](/pl/adr/006-kid-as-generated-guid/) - wartości `kid` publikowane w zestawie kluczy

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/004-token-key-bindings-domain/) | [Następny](/pl/adr/006-kid-as-generated-guid/)
