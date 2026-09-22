[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/007-default-signing-algorithm-rsa-4096/) | [Następny](/pl/adr/009-dynamic-plugin-discovery/)

# [ADR-008] Standaryzuj błędy API poprzez kontrakt odpowiedzi na błąd w Core

*2026-08* | Status: accepted

**Tag:** #adr_008

**Date:** 2026-08-26

**Scope:** AuthKit.Core

## Kontekst

Hosty (REST, gRPC, wtyczki) zgłaszają awarie klientom API i potrzebują spójnego kształtu, aby konsumenci mogli parsować błędy programowo. Rdzeń definiuje już bazę `DomainException` dla naruszeń reguł oraz `ErrorMetadataOptions` dla linków dokumentacyjnych.

## Problem

Bez współdzielonego kontraktu błędów należącego do rdzenia każdy host wymyśla własny schemat JSON, nazwy pól i obsługę znaczników czasu, fragmentując obsługę błędów po stronie klientów i łamiąc interoperacyjność pomiędzy powierzchniami API.

## Decyzja

Rdzeń jest właścicielem standaryzowanego DTO `ErrorResponse` z polami JSON `error` i `error_description` (zgodnymi z nazewnictwem błędów OAuth2) oraz znacznikiem czasu UTC `timestamp`, a także bazą `DomainException`, którą hosty mapują na ten kontrakt. Odwołania dokumentacyjne błędów są scentralizowane w `ErrorMetadataOptions.DocsBaseUrl`.

### ErrorResponse

**Obowiązki:**

- Prezentuje stabilny, kliencki kształt błędu (`error`, `error_description`, `timestamp`).
- Pozostaje przyjazny serializacji i agnostyczny względem hosta.

### DomainException

**Obowiązki:**

- Działa jako wspólna baza naruszeń reguł domenowych.
- Daje hostom jeden typ do przechwytywania i tłumaczenia na `ErrorResponse`.

### Uzasadnienie projektowe

- Jeden kontrakt utrzymuje jednolite parsowanie błędów przez klientów we wszystkich hostach.
- Nazwy pól maksymalizują kompatybilność ze standardowymi klientami.
- Znacznik czasu UTC wspomaga korelację i audyt bez logiki specyficznej dla hosta.

## Odrzucone

- Pozwolenie każdemu hostowi na definiowanie własnego kształtu JSON błędów.
- Zwracanie klientom surowego tekstu wyjątków lub śladów stosu.
- Pomijanie znacznika czasu lub linku dokumentacyjnego w kontrakcie.

## Konsekwencje

Klienci otrzymują przewidywalny format błędów, a hosty współdzielą jedną ścieżkę tłumaczenia awarii domenowych. Kosztem jest utrzymywanie `ErrorResponse` i `DomainException` w synchronizacji w miarę pojawiania się nowych przypadków błędów.

## Powiązane

- [ADR-014](/pl/adr/014-error-responses-via-middleware/) - host renderuje ten kontrakt jako ProblemDetails

[Strona ADR](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/007-default-signing-algorithm-rsa-4096/) | [Następny](/pl/adr/009-dynamic-plugin-discovery/)
