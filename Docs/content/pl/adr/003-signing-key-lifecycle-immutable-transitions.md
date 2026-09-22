[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/002-encrypt-keystore-at-rest/) | [Następny](/pl/adr/004-token-key-bindings-domain/)

# [ADR-003] Modeluj cykl życia klucza podpisującego jako niezmienne przejścia stanu

*2026-08* | Status: accepted

**Tag:** #adr_003

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement.Entity

## Kontekst

Klucz podpisujący przechodzi przez cykl życia: jest generowany, staje się ważny do podpisywania, może zostać zrotowany, wygasa i może zostać unieważniony. Wiele części systemu musi zadawać jedno, wiarygodne pytanie „czy ten klucz może teraz podpisywać?" bez ponownego implementowania reguł cyklu życia.

## Problem

Jeśli stan cyklu życia znajduje się wyłącznie w magazynie albo jest mutowany w miejscu przez usługi, reguły aktywacji, wygasania i unieważniania ulegają duplikacji i rozjeżdżają się. Mutowalne encje utrudniają też rozumowanie o współbieżnych operacjach na kluczach oraz o audycie.

## Decyzja

Klucz podpisujący jest modelowany jako niezmienny rekord `SigningKey`. Zmiany cyklu życia `Activate`, `Revoke`, `Expire` zwracają nową instancję poprzez wyrażenia `with` zamiast mutować stan, a pojedynczy predykat `IsValidForSigning(now)` ocenia aktywację, unieważnienie i wygaśnięcie względem podanego znacznika czasu.

### SigningKey

**Obowiązki:**

- Przenosi identyfikator klucza, klucz publiczny (PEM), zaszyfrowany materiał prywatny, algorytm oraz znaczniki czasu cyklu życia.
- Ujawnia `Activate`, `Revoke` i `Expire` jako czyste przejścia wytwarzające nowy rekord.
- Udostępnia `IsValidForSigning(DateTime now)` jako autorytatywne sprawdzenie ważności.

### Uzasadnienie projektowe

- Niezmienność sprawia, że przejścia są łatwe do testowania i wolne od ukrytych efektów ubocznych.
- Jeden predykat ważności zapobiega kopiowaniu logiki cyklu życia pomiędzy magazyn, host i powiązania.
- Ocena oparta na znaczniku czasu zachowuje determinizm reguły i niezależność od założeń zegara ściennego wewnątrz encji.

## Odrzucone

- Mutowalna encja z publicznymi setterami zmienianymi bezpośrednio przez usługi.
- Przechowywanie stanu cyklu życia wyłącznie w bazie danych i ponowne wyliczanie reguł w każdym konsumencie.
- Osadzanie logiki aktywacji/wygasania/unieważniania wewnątrz `JwtKeyStore` zamiast w encji.

## Konsekwencje

Zachowanie cyklu życia jest scentralizowane i spójne, a przejścia są audytowalne jako kopie wartości. Kosztem jest bogatszy kształt rekordu oraz dyscyplina zawsze używania zwróconej instancji zamiast oryginału.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - magazyn kluczy konsumujący cykl życia
- [ADR-005](/pl/adr/005-public-keys-via-jwks/) - JWKS wyklucza unieważnione klucze na podstawie tego stanu

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/002-encrypt-keystore-at-rest/) | [Następny](/pl/adr/004-token-key-bindings-domain/)
