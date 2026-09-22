[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/022-plugin-configuration-context-and-builder/) | [Następny](/pl/adr/024-plugin-lifecycle-and-hosted-services/)

# [ADR-023] Integracja endpointów i middleware wtyczek poprzez jawne hooki potoku hosta

*2026-09* | Status: accepted

**Tag:** #adr_023

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Kontekst

Wtyczki muszą wnosić endpointy i middleware żądań, lecz kolejność middleware ASP.NET Core jest częścią zachowania aplikacji. Kolejność odkrywania, kolejność systemu plików ani kolejność ładowania złożeń nie mogą decydować, gdzie działa kod wtyczki.

## Problem

Pierwotny kontrakt eksponował wyłącznie `MiddlewareType`, co dawało jeden niejawny slot middleware i brak hooka rejestracji endpointów. Wtyczki nie mogły jawnie umieszczać middleware względem routingu, uwierzytelniania, autoryzacji ani wykonywania endpointów.

## Decyzja

Kontrakt dodaje opcjonalne domyślne hooki:

- `MapEndpoints(IEndpointRouteBuilder)` dla normalnego routingu endpointów ASP.NET Core;
- `ConfigureApplication(IApplicationBuilder)` dla konfiguracji aplikacji wtyczki;
- `ConfigurePipeline(IApplicationBuilder, PluginPipelinePosition)` dla jawnie pozycjonowanego middleware;
- `PipelinePosition`, używające silnie typowanego wyliczenia `PluginPipelinePosition`.

Wspierane pozycje to `BeforeRouting`, `AfterRouting`, `BeforeAuthentication`, `AfterAuthentication`, `BeforeAuthorization`, `AfterAuthorization`, `BeforeEndpoints` oraz `AfterEndpoints`.

Host stosuje hooki do prawdziwego buildera aplikacji i buildera tras endpointów. Wtyczki na tej samej pozycji są porządkowane po stabilnym `Plugin.Id`, niezależnie od kolejności odkrywania. `AfterEndpoints` działa po mapowaniu endpointów REST i gRPC.

Istniejące zachowanie `MiddlewareType` pozostaje dostępne w swoim pierwotnym slocie. Jeśli wtyczka implementuje `ConfigureApplication` lub `ConfigurePipeline`, host nie rejestruje dodatkowo jej `MiddlewareType`, zapobiegając przypadkowej podwójnej rejestracji middleware. Wyjątki hooków i nieprawidłowe pozycje są zgłaszane jawnie.

### Uzasadnienie projektowe

- Hooki endpointów używają normalnego systemu routingu ASP.NET Core, zachowując metadane endpointów, autoryzację, uwierzytelnianie, odkrywanie OpenAPI oraz selekcję endpointów.
- Skończone wyliczenie eksponuje znaczące etapy potoku bez czynienia każdej wewnętrznej implementacji middleware publiczną zależnością.
- Sortowanie po stabilnym ID wtyczki czyni porządek na równych pozycjach deterministycznym i testowalnym.
- Domyślne elementy interfejsu utrzymują zgodność źródłową wtyczek używających wyłącznie `MiddlewareType`.

## Odrzucone

- Utrzymywanie porządku middleware równego kolejności odkrywania wtyczek jest niedeterministyczne.
- Dowolne pozycje znakowe są słabo typowane i nie dają się wiarygodnie walidować.
- Równoległy router endpointów omijałby metadane i selekcję endpointów ASP.NET Core.
- Ciche przenoszenie nieprawidłowych pozycji na koniec ukrywałoby błędy konfiguracji wtyczek.

## Konsekwencje

Wtyczki mogą uczestniczyć w potoku endpointów i middleware hosta bez modyfikowania kodu startowego hosta. Umiejscowienie w potoku jest jawne i przeglądalne. Autorzy wtyczek muszą wybrać wspierany etap przy użyciu `ConfigurePipeline`; host posiada granice etapów i deterministyczny porządek.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - odkrywanie wtyczek i granica kontraktu
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - ładowanie wtyczek i starszy slot middleware
- [Issue #9](https://github.com/AuthKits/AuthKit.Server/issues/9) - wymagania endpointów i potoku aplikacji

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/022-plugin-configuration-context-and-builder/) | [Następny](/pl/adr/024-plugin-lifecycle-and-hosted-services/)
