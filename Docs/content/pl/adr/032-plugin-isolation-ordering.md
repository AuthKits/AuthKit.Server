[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/031-plugin-discovery-manifest-gate/) | [Następny]()

# [ADR-032] Izolacja Pluginów We Własnych Kontekstach Ładowania Z Deterministyczną Kolejnością

*2026-09* | Status: accepted

**Tag:** #adr_032

**Date:** 2026-09-29

**Scope:** Ładowanie pluginów hosta (izolacja, kolejność, cache discovery)

## Context

Wszystkie pluginy lądowały w domyślnym `AssemblyLoadContext`, więc konfliktujące zależności przechodnie zwijały się do jednego uniwersum: która kopia wygrała, reszta padała z `TypeLoadException` albo cichym zbindowaniem złej wersji. Kolejność ładowania szła za kolejnością discovery, więc zależności rutynowo przegrywały z dependentami. Każdy restart płacił pełne rediscovery.

## Problem

Jeden współdzielony kontekst nie pomieści konfliktujących wersji zależności, a kolejność plików to nie kolejność ładowania. Bez izolacji każdy nowy plugin ryzykuje wszystkie pozostałe; bez kolejności krawędzie zależności to loteria; bez cache start powtarza całą pracę discovery z sondowaniem assembly włącznie.

## Decision

- Każdy plugin ładuje się do własnego kolekcjonowalnego `AssemblyLoadContext` (`PluginLoadContext`), wpiętego w kontraktowy `LoadedPlugin` — nigdy w `DiscoveredPlugin`. Kolekcjonowalność pod przyszły unload; sam loader nigdy nie zwalnia.
- Współdzielenie wygrywa regułą, po kolei: jawne współdzielone kontrakty (`AuthKit.Plugins.Abstractions`, `Grpc.Core.Api`, `Google.Protobuf`), wszystko już załadowane przez default, wszystko dostarczone z katalogiem hosta. Reszta idzie prywatnie z katalogu pluginu. Dzięki temu plugin zawsze widzi hostowe typy `Core` (tożsamość w DI trzyma) i nigdy własnej kopii `Interceptor`.
- Zaakceptowane pluginy ładują się w kolejności topologicznej Kahna: najpierw zależności, remisy po `Priority` rosnąco, potem po kolejności rejestracji, bez dosortowywania na końcu. Nieznane Id zależności i cykle to błąd startu; dependenci niedostępnych odpadają jako dependency-unavailable z propagacją.
- Wyniki discovery cache'ujemy w hostowym pliku (manifest + lokalizacja + fingerprint źródeł + wersja schematu; nigdy konteksty, typy ani instancje). Nieświeży lub zepsuty cache to miss, nigdy błąd.
- Bramka kompatybilności czyta efektywna flagę włączenia (manifest `IsEnabled` AND z configiem hosta, który może wyłączyć, ale nigdy włączyć) przed kolejkowaniem, więc wyłączone pluginy nigdy nie są szeregowane, izolowane ani ładowane.

### Design Rationale

- Zwracanie null (fallback do default) dla współdzielonych assembly trzyma jedno uniwersum typów dla kontraktów, a prywatne uniwersa rozjeżdżają się per plugin — dokładnie ta własność, której potrzebują DI i `is`.
- Reguła katalogu hosta (nie tylko już-załadowane) zamyka dziurę kolejności startu: ładowanie leci przed pierwszym użyciem gRPC/DI, więc sama jawna lista i tak duplikowałaby kopie na zimnym hoście.
- Kahn z priorytetem i rejestracją jest deterministyczny dla danego zbioru i czytelny w logach; kolejność discovery zostaje ostatecznym tiebreakiem, nigdy strategią.
- Cache trzyma fingerprinty, nie zaufanie do wyników: każda zmiana inputu (manifest albo bajty dll) unieważnia, więc przeterminowane wpisy nie ładują.

## Rejected

- Jeden współdzielony kontekst z unifikacją wersji: wymusza jedną wersję zależności na wszystkich pluginach i hoście — czyli oryginalny problem.
- Kopiowanie hostowych assembly per katalog pluginu: duplikuje typy kontraktów per ALC i cicho łamie tożsamość w DI.
- Szeregowanie po discovery albo pełne dosortowanie po priorytecie na końcu: pierwsze jest niedeterministyczne, drugie łamie krawędzie zależności.
- Rozproszony albo tylko in-memory cache: hostowy plik przeżywa restarty bez infrastruktury; cache'owanie aktywowanych instancji jawnie poza zakresem.

## Consequences

- Solucje pluginów nie mogą liczyć na prywatne ładowanie assembly dostarczanych przez hosta; hostowe wygrywa regułą i plugin dostaje kopię hosta.
- Każdy katalog pluginu dalej wymaga manifestu (ADR-031); tożsamość manifestu kluczem sortowania i walidacji.
- `PluginLoadResult` rozróżnia wyniki terminalne (loaded / skipped-disabled / rejected / invalid), więc każdy kandydat jest wyjaśnialny.
- Przyszła praca (orkiestracja unloadu, sufit wersji, reguły capabilities) wpina się w `PluginLoadContext`, `CompatibilityGate` albo `DependencyGraph` bez ruszania discovery.

## Powiązane

- [ADR-031](/pl/adr/031-plugin-discovery-manifest-gate/) - discovery manifestów z bramką kompatybilności przed ładowaniem
- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - discovery pluginów i granica kontraktu
- [ADR-028](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/) - kontrakt pluginu i architektura dynamicznego ładowania

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/031-plugin-discovery-manifest-gate/) | [Następny]()
