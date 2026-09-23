[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/010-plugin-loading-from-directory/) | [Następny](/pl/adr/012-token-key-bindings-persisted-in-marten/)

# [ADR-011] Utrzymywanie zaszyfrowanego magazynu kluczy jako singletonowego dokumentu Marten

*2026-08* | Status: accepted

**Tag:** #adr_011

**Date:** 2026-08-26

**Scope:** Host.KeyManagement.Repositories

## Kontekst

ADR-001 definiuje `IKeyStoreRepository` jako czysty odbiornik utrwalania, a ADR-002 stanowi, że przekazywane są mu wyłącznie zaszyfrowane bajty magazynu kluczy. Host musi zdecydować, gdzie te zaszyfrowane bajty faktycznie będą przechowywane.

## Problem

Zaszyfrowany magazyn kluczy musi być trwały pomiędzy restartami, ale repozytorium nie może przejmować kryptografii, a magazyn musi być adresowany w prosty sposób dla całego serwera istnieje dokładnie jeden magazyn kluczy.

## Decyzja

`KeyStoreRepository` utrwala magazyn kluczy jako pojedynczy dokument Marten o stałym identyfikatorze (`"singleton"`), przy użyciu lekkich sesji. Przechowuje wyłącznie zaszyfrowany ładunek (`KeystoreDocument.EncryptedData`); nigdy go nie szyfruje, nie odszyfrowuje ani nie interpretuje. Podczas odczytu zwraca `Memory<byte>.Empty`, gdy dokument nie istnieje; podczas zapisu wstawia lub aktualizuje singletonowy dokument, najpierw kopiując bajty wywołującego do nowej tablicy.

### KeyStoreRepository

**Obowiązki:**

- Odczyt/zapis zaszyfrowanego magazynu kluczy jako jednego dokumentu Marten identyfikowanego stałym identyfikatorem.
- Pozostawanie agnostycznym wobec kryptografii i używanie wyłącznie lekkich sesji.

### Uzasadnienie projektowe

- Singletonowy dokument odpowiada modelowi „jeden magazyn kluczy na serwer” i eliminuje zarządzanie kluczami/wyszukiwaniem.
- Utrzymywanie repozytorium jako prostego magazynu zaszyfrowanych bajtów respektuje granicę z ADR-001/ADR-002.
- Marten zapewnia trwałe, transakcyjne utrwalanie, z którego reszta hosta (Wolverine, wtyczki) już korzysta.

## Odrzucone

- Osobny plik płaski lub niestandardowy magazyn blobów poza Marten.
- Wiele dokumentów magazynu kluczy lub dokumenty na klucz.
- Pozwalanie repozytorium na wykonywanie szyfrowania lub odszyfrowywania.

## Konsekwencje

Materiał kluczowy przetrwa restarty w tym samym magazynie co reszta danych hosta, a rotacja (ADR-001) jest po prostu aktualizacją (upsert) singletonu. Kosztem jest twarda zależność od Marten/Postgres dla dostępności kluczy oraz dyscyplina, że repozytorium nigdy nie zagląda do ładunku.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - abstrakcja magazynu kluczy utrwalana tutaj
- [ADR-002](/pl/adr/002-encrypt-keystore-at-rest/) - przechowywane są wyłącznie zaszyfrowane bajty
- [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) - Marten jako magazyn hosta

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/010-plugin-loading-from-directory/) | [Następny](/pl/adr/012-token-key-bindings-persisted-in-marten/)
