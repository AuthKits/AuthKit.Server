[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/) | [Następny]()

# [ADR-029] Eksponowanie ustrukturyzowanych i anulowalnych wyników kondycji wtyczek

*2026-09* | Status: accepted

**Tag:** #adr_025

**Date:** 2026-09-13

**Scope:** AuthKit.Plugins.Abstractions + Host

## Kontekst

Wtyczki mogą zależeć od kilku niezależnych komponentów, takich jak baza danych, cache,
zewnętrzne API albo kolejka komunikatów. Pojedyncza boolowska wartość kondycji nie zachowuje
informacji, który komponent jest zdegradowany ani dlaczego kontrola zawiodła. Kontrole kondycji mogą też wykonywać
asynchroniczne I/O i muszą zatrzymywać się, gdy żądanie lub host jest anulowany.

## Problem

Pierwotny kontrakt wtyczki zwracał `Task<bool> CheckHealthAsync(IServiceProvider)`.
Ten kształt traci pośrednie stany kondycji, informacje diagnostyczne, wielokrotne
obserwacje oraz anulowanie. Zastąpienie go bez ścieżki migracji
złamałoby istniejące implementacje wtyczek.

## Decyzja

`IAuthKitPlugin.CheckHealthAsync` zwraca:

```csharp
Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
    IServiceProvider services,
    CancellationToken cancellationToken = default)
```

`PluginHealthResult` zawiera silnie typowany `PluginHealthStatus`, opcjonalny
powód oraz opcjonalne dane diagnostyczne należące do wtyczki. `Healthy`, `Degraded` i
`Unhealthy` pozostają odrębne. Wtyczka może zwrócić jeden wynik albo wiele wyników,
przy czym każdy wynik reprezentuje niezależną obserwację.

Domyślna implementacja interfejsu zwraca jeden wynik `Healthy`, więc wtyczki, które
nie dostarczają własnej kontroli, pozostają poprawne. Host zachowuje listę wyników i
używa dostarczonego tokenu anulowania żądania. Anulowanie jest propagowane, a nie
zamieniane na spreparowany wynik kondycji.

## Odrzucone

- Zachowanie `bool` odrzuciłoby stan zdegradowany i diagnostykę.
- Zwinięcie wielu wyników wewnątrz wtyczki uczyniłoby agregację hosta stratną.
- Wnioskowanie statusu z `Reason` lub `Data` uczyniłoby kontrakt słabo typowanym.
- Ciche zastępowanie anulowania przez `Healthy` lub `Unhealthy` ukryłoby
  niekompletną kontrolę.
- Dodanie drugiej metody kondycji pozostawiłoby dwa konkurujące publiczne kontrakty.

## Konsekwencje

Konsumenci kondycji muszą obsługiwać listę ustrukturyzowanych wyników i definiować agregację
jawnie. Endpoint kondycji hosta raportuje najwyższy status istotności, zachowując
każdy wynik wtyczki w odpowiedzi. Klucze diagnostyczne specyficzne dla wtyczki
pozostają rozszerzalne, lecz nie nadpisują `Status`.

Zmiana jest łamiąca źródłowo dla wtyczek implementujących starą metodę `Task<bool>`;
dostarczone wtyczki i przykładowy manifest są migrowane razem. Walidator
kontraktu wywołuje nową metodę i odrzuca pustą kolekcję wyników.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - kontrakt wtyczki i dynamiczne ładowanie
- [ADR-024](/pl/adr/024-plugin-lifecycle-and-hosted-services/) - integracja cyklu życia wtyczki
- [Issue #14](https://github.com/AuthKits/AuthKit.Server/issues/14) - ustrukturyzowany wynik kondycji wtyczki
- [Issue #15](https://github.com/AuthKits/AuthKit.Server/issues/15) - wiele wyników i anulowanie

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/) | [Następny]()
