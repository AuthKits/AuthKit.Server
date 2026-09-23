[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/023-plugin-application-pipeline-hooks/) | [Następny]()

# [ADR-024] Mostkowanie hooków cyklu życia wtyczek do standardowego cyklu życia hosta .NET

*2026-09* | Status: accepted

**Tag:** #adr_024

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host

## Kontekst

Wtyczki mogą musieć inicjalizować zasoby uruchomieniowe, wykonywać pracę po starcie, zwalniać zewnętrzne rejestracje podczas zamykania albo wnosić usługi tła. Statyczna rejestracja usług nie potrafi reprezentować tych operacji bezpiecznie.

## Problem

Bez jawnych hooków cyklu życia wtyczki potrzebowałyby kodu startowego specyficznego dla hosta albo własnych harmonogramów usług hostowanych. Ręczne wywoływanie usług tła wtyczek omijałoby też standardowy cykl życia hosta .NET i jego semantykę anulowania.

## Decyzja

`IAuthKitPlugin` eksponuje addytywne domyślne elementy:

- `OnStartingAsync(CancellationToken)`;
- `OnStartedAsync(CancellationToken)`;
- `OnStoppingAsync(CancellationToken)`;
- `GetHostedServices()` zwracające nie-null `IReadOnlyList<IHostedService>`.

Host używa jednego mostka `PluginLifecycleHostedService` rejestrowanego poprzez normalny kontener DI. porządkuje wtyczki po stabilnym `Plugin.Id`:

- `OnStartingAsync` działa w porządku rosnącym podczas startu usługi hostowanej;
- `OnStartedAsync` działa po `ApplicationStarted` i wyłącznie po udanym starcie;
- `OnStoppingAsync` działa w odwrotnym porządku, gdy zasygnalizowane jest `ApplicationStopping`.

Dostarczone przez wtyczki usługi hostowane są rejestrowane jako singletonowe instancje `IHostedService` przed startem hosta. Ich metody `StartAsync` i `StopAsync` są więc wywoływane przez standardowego hosta .NET, a nie przez kod AuthKit. Wyniki null, instancje usług null, podwójna rejestracja oraz wyjątki cyklu życia są odrzucane jawnie. Błędy cyklu życia zawierają w rzucanym wyjątku ID wtyczki i etap cyklu życia.

### Uzasadnienie projektowe

- Standardowa integracja `IHostedService` zachowuje startowe, zamykające, anulujące i dispose'owe zachowanie frameworka.
- Jeden mostek cyklu życia zapobiega podwójnemu wywoływaniu hooków i unika własnego harmonogramu.
- Porządek po stabilnym ID czyni start i zamykanie deterministycznymi niezależnie od kolejności odkrywania.
- Domyślne elementy interfejsu zachowują zgodność dla wtyczek niepotrzebujących zachowania cyklu życia.

## Odrzucone

- Ręczne wywoływanie `StartAsync` i `StopAsync` usług hostowanych stworzyłoby drugą implementację cyklu życia.
- Tworzenie kolejnego dostawcy usług lub zakresu usług rozdzieliłoby zależności wtyczek od kontenera DI aplikacji.
- Ignorowanie wyjątków cyklu życia pozwoliłoby hostowi raportować wtyczkę jako zdrową, gdy inicjalizacja zawiodła.
- Używanie kolejności odkrywania uzależniałoby zachowanie cyklu życia od wyliczania systemu plików lub złożeń.

## Konsekwencje

Błędy cyklu życia wtyczek zgłaszane są jawnie poprzez ścieżkę startu/zamykania hosta. Autorzy wtyczek mogą używać hooków świadomych anulowania do inicjalizacji i czyszczenia, podczas gdy praca długotrwała należy do implementacji `IHostedService` zwracanych przez `GetHostedServices()`. Istniejące wtyczki niezwracające usług hostowanych i nieimplementujące hooków pozostają poprawne dzięki bezpiecznym domyślnym.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - odkrywanie wtyczek i granica kontraktu
- [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) - cykl życia infrastruktury hosta
- [Issue #10](https://github.com/AuthKits/AuthKit.Server/issues/10) - wymagania cyklu życia i usług hostowanych

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/023-plugin-application-pipeline-hooks/) | [Następny]()
