[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/006-kid-as-generated-guid/) | [Następny](/pl/adr/008-standardized-error-response/)

# [ADR-007] Domyślnym algorytmem podpisywania jest RSA-4096 z RS256

*2026-08* | Status: accepted

**Tag:** #adr_007

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Kontekst

Generator kluczy i magazyn kluczy muszą zgadzać się co do algorytmu i siły klucza używanych do wytwarzania i weryfikowania JWT. `IKeyGenerator.Generate` przyjmuje konfigurowalne `rsaBits`, a `JwtKeyStore` buduje `SigningCredentials` ze stałą algorytmu.

## Problem

Bez zadeklarowanego domyślnego wywołujący mogą wybierać słabe rozmiary kluczy lub niedopasowane algorytmy, a magazyn i generator mogą się rozjechać, wytwarzając klucze, których magazyn nie potrafi użyć ani spójnie zweryfikować.

## Decyzja

Domyślny klucz podpisujący to RSA z 4096-bitowym modułem, a podpisy używają `SecurityAlgorithms.RsaSha256` (RS256). Domyślny rozmiar jest wyrażony jednokrotnie jako parametr `rsaBits = 4096` w `IKeyGenerator.Generate` i w `JwtKeyStore.RotateAsync`, natomiast algorytm jest ustalony w momencie tworzenia poświadczeń; rozmiar pozostaje nadpisywalny przez wywołującego na potrzeby rotacji.

### Uzasadnienie projektowe

- RSA-4096 daje konserwatywny margines bezpieczeństwa dla długowiecznych kluczy podpisujących.
- RS256 jest szeroko wspierany przez weryfikatory JWT i naturalnie paruje się z kluczami RSA oraz JWKS publikowanym przez magazyn.
- Centralizacja domyślnego utrzymuje generator i magazyn w zgodzie i sprawia, że przyszła zmiana algorytmu to edycja w jednym miejscu.

## Odrzucone

- Domyślne RSA-2048 lub mniejsze „ze względu na wydajność".
- Uczynienie ECDSA domyślnym algorytmem.
- Zakodowanie algorytmu na sztywno w magazynie przy pozostawieniu generatorowi swobody wyboru innego.

## Konsekwencje

Nowe klucze są konsekwentnie silne i weryfikowalne w całym ekosystemie. Kosztem jest większy rozmiar klucza (więcej CPU na podpis) oraz potrzeba ponownego rozważenia domyślnego, jeśli zmieni się model zagrożeń lub wsparcie standardów.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - magazyn kluczy budujący poświadczenia
- [ADR-006](/pl/adr/006-kid-as-generated-guid/) - identyfikator generowany wraz z kluczem

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/006-kid-as-generated-guid/) | [Następny](/pl/adr/008-standardized-error-response/)
