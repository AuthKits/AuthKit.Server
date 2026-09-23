[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/021-swagger-serving-via-reflection/) | [Następny](/pl/adr/023-plugin-application-pipeline-hooks/)

# [ADR-022] Rozszerzenie konfiguracji wtyczki o builder hosta i zakresowy kontekst

*2026-09* | Status: accepted

**Tag:** #adr_022

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Kontekst

Wtyczki konfigurowały wcześniej usługi poprzez `ConfigureServices(IServiceCollection, IConfiguration)`. To wystarczało do rejestracji, lecz nie eksponowało faktycznego buildera hosta ani stabilnego, specyficznego dla wtyczki kontekstu konfiguracji.

## Problem

Dodanie abstrakcyjnych elementów do `IAuthKitPlugin` złamałoby istniejące wtyczki. Przekazywanie kolejnych indywidualnych zależności hosta utrudniłoby też ewolucję kontraktu i zachęcałoby wtyczki do polegania na wewnętrznych mechanizmach hosta.

## Decyzja

Kontrakt wtyczki eksponuje addytywne domyślne elementy interfejsu:

- `ConfigureServices(IHostApplicationBuilder, IConfiguration)` dla wtyczek potrzebujących prawdziwego buildera hosta AuthKit.
- `ConfigureServices(IServiceCollection, AuthKitPluginContext)` dla wtyczek potrzebujących stabilnej tożsamości wtyczki i kontekstu konfiguracji.
- Istniejące `ConfigureServices(IServiceCollection, IConfiguration)` pozostaje poprawne dla starszych wtyczek.

`AuthKitPluginContext` żyje w główniej przestrzeni nazw `AuthKit.Plugins.Abstractions` i eksponuje:

- stabilny `PluginId`;
- `PluginName`;
- zakresową dla wtyczki `Configuration` z `Plugins:{PluginId}` z rezerwą do nazwy;
- pełną `ApplicationConfiguration` tylko do odczytu dla ustawień poziomu hosta.

Host używa jednego dyspozytora. Wybiera najpierw konfigurację kontekstową, potem konfigurację buildera hosta, a na końcu starsze przeciążenie. Dla wtyczki wywoływane jest tylko jedno przeciążenie, więc ścieżki zgodności nie mogą zarejestrować tych samych usług dwukrotnie.

### Uzasadnienie projektowe

- Domyślne implementacje interfejsu zachowują zgodność źródłową.
- Faktyczny `WebApplicationBuilder` jest przekazywany zamiast konstruowania izolowanego buildera.
- Zakresowa dla wtyczki konfiguracja zapobiega przypadkowemu odczytaniu ustawień innej wtyczki.
- Pełna konfiguracja aplikacji pozostaje dostępna jawnie bez tworzenia drugiego DI ani drugiego systemu konfiguracji.

## Odrzucone

- Uczynienie nowych przeciążeń abstrakcyjnymi złamałoby istniejące wtyczki.
- Konstruowanie osobnego buildera hosta odłączyłoby rejestracje od działającej aplikacji.
- Przekazywanie `IServiceProvider` przez kontekst wprowadziłoby zachowanie service-locator.
- Wywoływanie każdego przeciążenia powodowałoby podwójną rejestrację i niejednoznaczne zachowanie.

## Konsekwencje

Nowe wtyczki mogą optować za konfiguracją świadomą hosta albo zakresowymi danymi kontekstu. Istniejące wtyczki, takie jak DevTokens i DevTools, nadal używają swojej starszej implementacji bez zmian źródłowych. Dyspozytor jest zagadnieniem hosta, a publiczny kontrakt pozostaje niezależny od wewnętrznego programu ładującego wtyczki hosta.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - kontrakt wtyczki i dynamiczne odkrywanie
- [ADR-019](/pl/adr/019-plugin-metadata-attribute/) - deklaratywna tożsamość wtyczki używana przez kontekst
- [Issue #8](https://github.com/AuthKits/AuthKit.Server/issues/8) - wymagania buildera hosta i kontekstu wtyczki

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/021-swagger-serving-via-reflection/) | [Następny](/pl/adr/023-plugin-application-pipeline-hooks/)
