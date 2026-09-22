[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni]() | [Następny](/pl/adr/002-encrypt-keystore-at-rest/)

# [ADR-001] Scentralizuj zarządzanie kluczami podpisującymi poprzez abstrakcję głównego magazynu kluczy w Core

*2026-08* | Status: accepted

**Tag:** #adr_001

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Kontekst

Rdzeń (core) AuthKit jest właścicielem materiału kryptograficznego używanego do wystawiania i weryfikowania JWT. Udostępnia mały zestaw prymitywów `IKeyGenerator`, `IKeyEncryptor`, `IJwtKeyStore` i `IKeyStoreRepository` które łącznie obejmują tworzenie kluczy, szyfrowanie w spoczynku oraz utrwalanie. Warstwa hosta (REST, gRPC, wtyczki) musi mieć możliwość wybijania, rotowania i publikowania kluczy podpisujących bez przejmowania wiedzy o tym, jak te klucze są generowane, szyfrowane lub przechowywane.

## Problem

Host nie powinien zależeć od konkretnych formatów przechowywania kluczy, algorytmów szyfrowania ani parametrów generowania. Bez centralnej abstrakcji zarządzania kluczami należącej do rdzenia wiedza ta wycieka do hosta i do poszczególnych punktów końcowych: każdy konsument na nowo wymyśla sposób wytwarzania kluczy, ich ochrony na dysku oraz rozwiązywania aktywnego klucza podpisującego. Zmiana nazwy klucza, zmiana schematu szyfrowania lub podmiana backendu przechowywania staje się wówczas zmianą przekrojową bez jednego kontraktu, na którym można się oprzeć.

## Decyzja

Zarządzanie kluczami podpisującymi jest scentralizowane za abstrakcją rdzenia, która tworzy kontrakt pomiędzy generowaniem kluczy, szyfrowaniem i utrwalaniem z jednej strony a hostem z drugiej. Klucz podpisujący staje się stabilną encją rdzenia (`SigningKey`) rozwiązywaną poprzez magazyn kluczy, a nie surowym plikiem ani rekordem repozytorium znanym tylko jednemu punktowi końcowemu.

### IKeyGenerator

**Obowiązki:**

- Wytwarza nowe asymetryczne pary kluczy do podpisywania (`GenerateAsync`).
- Ujawnia używany algorytm i parametry, aby reszta rdzenia mogła wnioskować o sile klucza.
- Pozostaje niezależny od zagadnień przechowywania i szyfrowania.

### IKeyEncryptor

**Obowiązki:**

- Szyfruje surowy materiał kluczy w celu ochrony w spoczynku (`EncryptAsync` / `DecryptAsync`).
- Izoluje algorytm szyfrowania (np. AES) od magazynu i generatora.
- Nigdy nie ujawnia materiału w postaci jawnej poza granicę rdzenia.

### IJwtKeyStore

**Obowiązki:**

- Rozwiązuje aktywny klucz podpisujący oraz zbiór opublikowanych kluczy publicznych (`GetActiveKeyAsync`, `GetPublicJwksAsync`).
- Koordynuje generator i szyfrator tak, aby utrwalane klucze były zawsze zaszyfrowane.
- Prezentuje hostowi klucze jako encje `SigningKey` oraz publiczne JWK, ukrywając szczegóły przechowywania.

### IKeyStoreRepository

**Obowiązki:**

- Utrwala i wczytuje zaszyfrowany rekord magazynu kluczy (`KeystoreOnDisk`, `KeyEntry`, `KeyMetadata`).
- Pozostaje czystą granicą utrwalania bez logiki kryptograficznej ani domenowej.

### Uzasadnienie projektowe

- Jeden kontrakt rdzenia sprawia, że decyzje kryptograficzne są testowalne, audytowalne i wymienialne bez dotykania hosta.
- Rozdzielenie generowania, szyfrowania i przechowywania pozwala każdemu zagadnieniu ewoluować niezależnie (aktualizacja algorytmu, zmiana backendu przechowywania, polityka rotacji) za stabilnymi interfejsami.
- Ujawnianie kluczy jako encji domenowych i publicznych JWK zapobiega sprzęganiu hosta z formatami dyskowymi i zmniejsza ryzyko błędów obsługi kluczy.

## Odrzucone

- Pozwolenie hostowi na bezpośrednie generowanie, szyfrowanie i utrwalanie kluczy za pomocą doraźnych wywołań.
- Sprzęganie wiedzy o kluczu podpisującym i szyfrowaniu z konkretnym kodem punktów końcowych.
- Jedna monolityczna usługa kluczy łącząca generowanie, szyfrowanie i przechowywanie w jednej klasie.
- Przechowywanie kluczy w postaci jawnej lub poleganie na sekretach zarządzanych przez hosta zamiast na szyfratorze rdzenia.

## Konsekwencje

Rdzeń staje się jedynym, przewidywalnym właścicielem cyklu życia kluczy podpisujących i łatwiej jest kontrolować kompletność kluczy, rotację oraz spójność algorytmów w całym hoście. Kosztem jest utrzymywanie abstrakcji rdzenia jako kontraktu i aktualizowanie jej przy każdym wprowadzeniu nowej capability zarządzania kluczami.

## Powiązane

- [ADR-002](/pl/adr/002-encrypt-keystore-at-rest/) - szyfrowanie bajtów magazynu kluczy w spoczynku
- [ADR-003](/pl/adr/003-signing-key-lifecycle-immutable-transitions/) - przejścia cyklu życia klucza podpisującego
- [ADR-005](/pl/adr/005-public-keys-via-jwks/) - odkrywanie kluczy publicznych poprzez JWKS
- [ADR-006](/pl/adr/006-kid-as-generated-guid/) - strategia identyfikatora klucza
- [ADR-007](/pl/adr/007-default-signing-algorithm-rsa-4096/) - domyślny algorytm podpisywania
- [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) - utrwalanie magazynu kluczy

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni]() | [Następny](/pl/adr/002-encrypt-keystore-at-rest/)
