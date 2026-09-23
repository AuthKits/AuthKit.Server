[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/005-public-keys-via-jwks/) | [Następny](/pl/adr/007-default-signing-algorithm-rsa-4096/)

# [ADR-006] Wyznaczaj identyfikator klucza JWT jako generowany GUID

*2026-08* | Status: accepted

**Tag:** #adr_006

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Kontekst

Każdy klucz podpisujący potrzebuje stabilnego identyfikatora, który występuje jako nagłówek JWT `kid`, indeksuje słownik kluczy w pamięci oraz łączy rekordy utrwalania i wpisy publicznych JWK. Generator tworzy klucze bez przypisanego zewnętrznego identyfikatora.

## Problem

Jeśli identyfikator jest znaczący (np. koduje klucz publiczny lub sekwencję), może się zepsuć przy rotacji klucza lub regeneracji jego materiału oraz ujawniać wewnętrzną strukturę. System potrzebuje jednego identyfikatora, który jest stabilny przez całe życie klucza we wszystkich reprezentacjach.

## Decyzja

Generator kluczy przypisuje każdemu nowemu kluczowi `kid` wytworzony przez `Guid.NewGuid().ToString("N")`. Ta sama wartość jest używana jako JWT `kid`, klucz `ConcurrentDictionary` w `JwtKeyStore`, `KeyMetadata.Kid` oraz pole JWKS `kid`, więc wszystkie reprezentacje są zakotwiczone w jednym identyfikatorze.

### Uzasadnienie projektowe

- Losowy GUID jest unikalny, nieprzezroczysty i stabilny przez cały czas życia klucza niezależnie od rotacji czy ponownego eksportu.
- Ponowne używanie jednego identyfikatora wszędzie usuwa logikę translacji pomiędzy utrwalaniem, pamięcią, JWT i JWKS.
- Żadna wewnętrzna struktura nie jest ujawniana stronom ufającym.

## Odrzucone

- Sekwencyjne lub znaczące identyfikatory ujawniające kolejność lub szczegóły wewnętrzne.
- Wyznaczanie `kid` z hasha klucza publicznego, co komplikuje rotację i ponowne eksporty.
- Pozwolenie wywołującym lub hostowi na zewnętrzne przypisywanie `kid`.

## Konsekwencje

Tożsamość klucza jest spójna i wolna od kolizji w całym systemie. Kosztem jest to, że `kid` nie niesie znaczenia czytelnego dla człowieka, więc debugowanie opiera się na metadanych, a nie na samym identyfikatorze.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - indeksowanie magazynu kluczy po `kid`
- [ADR-005](/pl/adr/005-public-keys-via-jwks/) - `kid` w publikowanym zestawie kluczy
- [ADR-007](/pl/adr/007-default-signing-algorithm-rsa-4096/) - algorytm sparowany z identyfikatorem

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/005-public-keys-via-jwks/) | [Następny](/pl/adr/007-default-signing-algorithm-rsa-4096/)
