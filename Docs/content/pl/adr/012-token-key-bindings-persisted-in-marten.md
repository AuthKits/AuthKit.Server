[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) | [Następny](/pl/adr/013-dual-rest-and-grpc-transport/)

# [ADR-012] Utrzymywanie powiązań tokenów z kluczami w Marten

*2026-08* | Status: accepted

**Tag:** #adr_012

**Date:** 2026-08-26

**Scope:** Host.TokenKeyBindings.Repositories

## Kontekst

ADR-004 czyni powiązania tokenów z kluczami podpisującymi domeną rdzenia, a ADR-011 utrwala sam zaszyfrowany materiał kluczy podpisujących jako singletonowy dokument Marten. Host musi zdecydować, gdzie przechowywany jest stan powiązań, aby przetrwał restarty wraz z kluczami, do których się odwołuje.

## Problem

Powiązania łączą tokeny deweloperskie z kluczami podpisującymi i wspierają rotację, aktualizacje kluczy publicznych oraz unieważnianie. Jeśli są trzymane wyłącznie w pamięci, znikają przy restarcie, wymuszając ponowne ustanawianie każdego powiązania token -> klucz i psując weryfikację już wydanych tokenów po ponownym wdrożeniu. Repozytorium musi przy tym pozostać wolne od logiki domenowej i używać tego samego magazynu, od którego zależy już reszta hosta.

## Decyzja

`KeyBindingRepository` utrwala każde `TokenKeyBinding` jako dokument Marten identyfikowany złożonym identyfikatorem zbudowanym z identyfikatora tokenu deweloperskiego i identyfikatora klucza podpisującego (`"{tokenId:N}:{signingKeyId}"`). Używa lekkich sesji Marten, przechowuje wyłącznie zserializowane powiązanie i nie wykonuje żadnych przejść ani operacji kryptograficznych.

### KeyBindingRepository

**Obowiązki:**

- Dodawanie/pobieranie/aktualizowanie/wyliczanie encji `TokenKeyBinding` poprzez dokumenty Marten.
- Adresowanie każdego powiązania poprzez złożoną tożsamość `(tokenId, signingKeyId)`.
- Pozostawanie czystą granicą utrwalania, jak `KeyStoreRepository` (ADR-011).

### KeyBindingDocument

**Obowiązki:**

- Przenoszenie identyfikatora dokumentu Marten oraz utrwalonego ładunku `TokenKeyBinding`.

### Uzasadnienie projektowe

- Trwałe utrwalanie zachowuje pochodzenie token -> klucz oraz stan unieważnień pomiędzy restartami, dzięki czemu wydane tokeny pozostają weryfikowalne po ponownym wdrożeniu.
- Ponowne użycie Marten (ADR-016) eliminuje drugi magazyn danych i utrzymuje powiązania spójne z magazynem kluczy transakcyjnie i operacyjnie.
- Złożony identyfikator dokumentu odpowiada naturalnej tożsamości powiązania i sprawia, że `GetAsync` oraz `ListByTokenAsync` są bezpośrednimi odczytami/zapytaniami.

## Odrzucone

- Osobny plik płaski lub niestandardowy magazyn blobów poza Marten.
- Pozwalanie repozytorium na wykonywanie przejść powiązań lub szyfrowania.

## Konsekwencje

Powiązania kluczy tokenów są trwałe i przeszukiwalne jak reszta danych hosta, a projekt pozostaje czysty implementacja `IKeyBindingRepository`. Kosztem jest twarda zależność od Marten/Postgres dla dostępności powiązań (ta sama zależność, która jest już wymagana przez magazyn kluczy) oraz dyscyplina, że repozytorium nigdy nie przegląda ani nie modyfikuje ładunku powiązania.

## Powiązane

- [ADR-004](/pl/adr/004-token-key-bindings-domain/) - domena utrwalana tutaj
- [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) - magazyn kluczy utrwalany w ten sam sposób
- [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) - Marten jako magazyn hosta

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) | [Następny](/pl/adr/013-dual-rest-and-grpc-transport/)
