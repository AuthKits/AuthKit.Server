[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/003-signing-key-lifecycle-immutable-transitions/) | [Następny](/pl/adr/005-public-keys-via-jwks/)

# [ADR-004] Traktuj powiązania tokenów deweloperskich z kluczami podpisującymi jako domenę Core

*2026-08* | Status: accepted

**Tag:** #adr_004

**Date:** 2026-08-26

**Scope:** AuthKit.Core.TokenKeyBindings

## Kontekst

Tokeny deweloperskie muszą być powiązane z konkretnym kluczem podpisującym RSA (i jego kluczem publicznym) użytym do podpisania ich JWT, aby weryfikację i rotację można było śledzić na token. To powiązanie jest pojęciem domenowym, a nie szczegółem implementacyjnym pojedynczego punktu końcowego.

## Problem

Bez pierwszoplanowego pojęcia powiązania mapowanie pomiędzy tokenami a kluczami podpisującymi jest rozproszone pomiędzy hosty i materiał kluczy, co utrudnia śledzenie rotacji, aktualizacji kluczy publicznych i unieważnień oraz prowadzi do niespójności pomiędzy powierzchniami API.

## Decyzja

Powiązania tokenów z kluczami są domeną rdzenia: rekord `TokenKeyBinding` przechwytuje `TokenId`, `SigningKeyId`, klucz publiczny, znacznik czasu `BoundAt` oraz flagę `Revoked`. `IKeyBindingService` stosuje zachowania domenowe (`CreateBindingAsync`, `RebindAsync`, `UpdatePublicKeyAsync`, `RevokeAsync`, `ListBindingsAsync`, `GetBindingAsync`), a `IKeyBindingRepository` jest czystą granicą utrwalania.

### TokenKeyBinding

**Obowiązki:**

- Reprezentuje niezmienne przez kopię powiązanie pomiędzy tokenem deweloperskim a kluczem podpisującym.
- Ujawnia `Rebind`, `UpdatePublicKey` i `Revoke` jako przejścia zwracające nowy rekord z odświeżonym `BoundAt`.

### IKeyBindingService

**Obowiązki:**

- Orkiestruje operacje powiązań poprzez repozytorium.
- Wywołuje przejścia encji zamiast bezpośrednio mutować stan powiązania.

### IKeyBindingRepository

**Obowiązki:**

- Utrwala, wczytuje, aktualizuje i listuje powiązania.
- Pozostaje wolny od zachowań kryptograficznych i domenowych.

### Uzasadnienie projektowe

- Dedykowana domena sprawia, że pochodzenie klucza na token jest jawne i wspiera rotację bez łamania weryfikacji.
- Rozdzielenie serwisu (zachowanie) od repozytorium (utrwalanie) odzwierciedla projekt zarządzania kluczami i pozostaje testowalne.

## Odrzucone

- Kodowanie mapowania token -> klucz niejawnie wewnątrz magazynu kluczy.
- Pozwolenie hostom na zarządzanie powiązaniami poprzez doraźny dostęp do bazy danych.
- Bezpośrednie mutowanie pól powiązania z serwisu zamiast używania przejść encji.

## Konsekwencje

Pochodzenie kluczy tokenów, rotacja i unieważnianie są spójne i obserwowalne. Kosztem jest utrzymywanie drugiej domeny rdzenia obok zarządzania kluczami oraz dbanie o czystość granicy repozytorium.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - klucze podpisujące, do których odwołują się powiązania
- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - wtyczki konsumują powiązania poprzez tę domenę
- [ADR-012](/pl/adr/012-token-key-bindings-persisted-in-marten/) - utrwalanie powiązań

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/003-signing-key-lifecycle-immutable-transitions/) | [Następny](/pl/adr/005-public-keys-via-jwks/)
