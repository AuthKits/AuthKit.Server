[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/001-centralize-signing-key-management/) | [Następny](/pl/adr/003-signing-key-lifecycle-immutable-transitions/)

# [ADR-002] Szyfruj utrwalony materiał magazynu kluczy w spoczynku poprzez wymienialny szyfrator

*2026-08* | Status: accepted

**Tag:** #adr_002

**Date:** 2026-08-26

**Scope:** AuthKit.Core.KeyManagement

## Kontekst

Magazyn kluczy utrwala wrażliwy materiał prywatnych kluczy RSA, aby klucze podpisujące przetrwały restarty. Granica repozytorium (`IKeyStoreRepository`) jest celowo czystym ujściem utrwalania przechowuje i zwraca nieprzezroczyste bajty bez rozumienia ich zawartości.

## Problem

Materiał kluczy prywatnych nie może być nigdy zapisywany na dysku w postaci jawnej. Jednocześnie warstwa utrwalania musi pozostać nieświadoma kryptografii, aby można ją było podmieniać (plik, blob, baza danych) bez dotykania logiki bezpieczeństwa. System potrzebuje też jednego, wymienialnego miejsca do zmiany algorytmu szyfrowania, gdyby zmienił się model zagrożeń.

## Decyzja

Wszystkie bajty magazynu kluczy są szyfrowane przed utrwaleniem i odszyfrowywane po wczytaniu poprzez granicę `IKeyEncryptor`. Domyślna implementacja (`AesKeyEncryptor`) używa AES-256-CBC z 256-bitowym kluczem głównym podawanym podczas konstrukcji, generuje kryptograficznie losowy IV przy każdym wywołaniu szyfrowania oraz prefiksuje IV do szyfrogramu, aby można go było odzyskać podczas odszyfrowywania.

### IKeyEncryptor

**Obowiązki:**

- Szyfruje tekst jawny UTF-8 do nieprzezroczystego bloba bajtów (`Encrypt`).
- Odwraca operację i zwraca tekst jawny (`Decrypt`).
- Pozostaje bezstanowy i niezależny od repozytorium utrwalania.

### Uzasadnienie projektowe

- Umieszczenie szyfrowania w dedykowanej, wstrzykiwalnej granicy pozwala zmienić algorytm (np. na AES-GCM) bez modyfikowania magazynu ani repozytorium.
- Losowy IV na wywołanie zapobiega ponownemu użyciu pary klucz/IV i sprawia, że format dyskowy jest niedeterministyczny.
- Repozytorium pozostaje prostym magazynem bajtów, dzięki czemu backendy przechowywania są wymienne.

## Odrzucone

- Utrwalanie kluczy w postaci jawnej lub poleganie wyłącznie na uprawnieniach systemu plików.
- Pozwolenie `IKeyStoreRepository` na wykonywanie szyfrowania lub odszyfrowywania.
- Stały, wbudowany klucz skompilowany w złożeniu (assembly).
- Ponowne używanie jednego statycznego IV we wszystkich szyfrowaniach.

## Konsekwencje

Materiał kluczy jest poufny w spoczynku, a algorytm szyfrowania jest odizolowany za kontraktem. Dokumentacja wprost zaznacza, że AES-CBC zapewnia poufność bez uwierzytelnionej integralności, więc zaszyfrowany blob musi być chroniony przed manipulacją innym mechanizmem albo zmigrowany do trybu uwierzytelnionego.

## Powiązane

- [ADR-001](/pl/adr/001-centralize-signing-key-management/) - abstrakcja magazynu kluczy będąca właścicielem kontraktu
- [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) - miejsce utrwalania zaszyfrowanych bajtów

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/001-centralize-signing-key-management/) | [Następny](/pl/adr/003-signing-key-lifecycle-immutable-transitions/)
