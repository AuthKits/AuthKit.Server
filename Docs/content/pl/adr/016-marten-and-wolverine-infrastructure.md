[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/015-keycloak-external-jwt-authority/) | [Następny]()

# [ADR-016] Używanie Marten i Wolverine jako infrastruktury hosta

*2026-08* | Status: accepted

**Tag:** #adr_016

**Date:** 2026-08-26

**Scope:** Host.Configuration

## Kontekst

Host potrzebuje magazynu dokumentów dla danych trwałych (w tym magazynu kluczy z ADR-011 i dokumentów wtyczek) oraz szkieletu obsługi komunikatów/poleceń, który potrafi także odkrywać procedury obsługi wnoszone przez dynamicznie ładowane wtyczki (ADR-010).

## Problem

Wybór prymitywów utrwalania i przesyłania komunikatów wpływa na każdą warstwę: wtyczki muszą móc uczestniczyć w tym samym potoku poleceń i magazynie dokumentów, a walidacja powinna być częścią przetwarzania komunikatów, a nie być rozproszona. Ręczne budowanie dyspozytora lub mieszanie ORM-ów komplikuje tę integrację.

## Decyzja

Host standaryzuje Marten jako magazyn dokumentów oraz Wolverine jako szkielet przesyłania komunikatów/poleceń, zintegrowane ze sobą:

- `ConfigureMarten` łączy z Marten (ciąg połączenia `Marten`), schemat `AutoCreate.All`, lekkie sesje oraz `IntegrateWithWolverine()`; `IDocumentSession` jest udostępniany jako usługa o zakresie (scoped).
- `ConfigureWolverine` używa Wolverine z FluentValidation zintegrowanym z przetwarzaniem komunikatów oraz `IncludeEventHandlers(plugins)`, dzięki czemu odkrywane są procedury obsługi ze złożenia Core i dynamicznie ładowanych złożeń wtyczek.

### Uzasadnienie projektowe

- Integracja Marten + Wolverine pozwala współdzielić transakcje między zapisami dokumentów a obsługą komunikatów oraz mieć jedną historię konfiguracji.
- Odkrywanie procedur obsługi ze złożeń wtyczek oznacza, że rozwiązania (np. DevTokens) wpinają się w ten sam potok poleceń bez zmian hosta.
- FluentValidation w potoku Wolverine centralizuje walidację poleceń przed uruchomieniem procedur obsługi.

## Odrzucone

- Entity Framework Core lub niestandardowy ORM jako główny magazyn.
- Ręcznie pisany dyspozytor poleceń zamiast Wolverine.
- Osobny magazyn zdarzeń/komunikatów odłączony od magazynu dokumentów.

## Konsekwencje

Wtyczki i Core współdzielą jeden model utrwalania i przesyłania komunikatów, co utrzymuje spójność procedur obsługi, dokumentów i magazynu kluczy. Kosztem jest wymagana zależność Postgres/Marten oraz powierzchnia nauki dwóch frameworków; logowanie infrastrukturalne dla Wolverine/Marten/Npgsql jest celowo wyciszone, aby ograniczyć szum.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - procedury obsługi wtyczek odkrywane przez Wolverine
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - załadowane złożenia zasilają infrastrukturę
- [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) - magazyn kluczy przechowywany w Marten
- [ADR-012](/pl/adr/012-token-key-bindings-persisted-in-marten/) - powiązania przechowywane w Marten

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/015-keycloak-external-jwt-authority/) | [Następny](/pl/adr/017-api-key-credential-extraction-strategies/)
