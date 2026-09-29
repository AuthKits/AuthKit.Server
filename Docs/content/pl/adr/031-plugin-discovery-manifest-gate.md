[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/030-plugin-middleware-pipeline/) | [Następny]()

# [ADR-031] Odkrywanie Pluginów Przez Manifesty Z Bramką Kompatybilności Przed Ładowaniem

*2026-09* | Status: accepted

**Tag:** #adr_031

**Date:** 2026-09-24

**Scope:** AuthKit.Plugins.Abstractions + ładowanie pluginów hosta

## Context

Pluginy ładowała jedna statyczna procedura hosta: znajdowała assembly wejściowe, konstruowała instancję i dopiero potem czytała metadane z samej instancji. Host nie mógł rozumować o pluginie przed jego konstrukcją, więc plugin wymagający nowszego hosta padał późno (albo kładł start), wyłączone pluginy i tak były konstruowane, a zduplikowane tożsamości wychodziły daleko od przyczyny. Nie było szwu na własne discovery ani ładowanie.

## Problem

Metadane żyły tylko na skonstruowanej instancji `IAuthKitPlugin`, co zmuszało hosta do zbudowania obiektu przed jakąkolwiek decyzją o kompatybilności. Jedna procedura `LoadPlugins` mieszała discovery, kompatybilność, konstrukcję i walidację, więc żaden z tych etapów nie dał się wymienić, testować ani analizować w izolacji.

## Decision

- Podział pipeline na wymienne etapy z jawną własnością: `IPluginDiscoverer` znajduje kandydatów i czyta manifesty (nigdy nie aktywuje), `IPluginLoader` ładuje assembly i konstruuje instancje (nigdy nie ocenia kompatybilności), a pipeline hosta odpowiada za walidację, bramkę, spójność i aktywację wokół nich.
- Discoverer czyta `plugin.json` / `plugin.manifest` / `manifest.json` z dysku bez ładowania assembly, zwracając `DiscoveredPlugin` (manifest + nieprzezroczysta lokalizacja + błąd discovery). Kolejność etapów: błędy discovery → walidacja strukturalna → odrzucenie duplikatów Id → bramka kompatybilności → ładowanie → spójność manifest/instancja → walidacja kontraktu.
- Bramka kompatybilności działa przed ładowaniem na manifeście: `IsEnabled == false` cicho pomija, `HostVersion < MinHostVersion` twardo odrzuca (bez trybu warn-only — za nowy plugin grozi `MissingMethodException` / `TypeLoadException`). Brak `MinHostVersion` znaczy brak wpływu.
- Spójność manifest/instancja (`Id`, `Name`, `Version`, `IsEnabled`, równe zbiory `Capabilities`, `MinHostVersion`, `DependsOn`) to twardy błąd. Zduplikowane Id manifestów odrzucane deterministycznie (pierwszy po lokalizacji wygrywa).
- Wyniki zbiera hostowy `PluginLoadResult` (załadowane / pominięte-wyłączone / odrzucone / niepoprawne, z powodami) do logów startowych i diagnostyki; celowo nie jest częścią kontraktu pluginu.
- Manifest jest wymagany: katalogi bez czytelnego manifestu są niepoprawne i nigdy się nie ładują. Nie ma ścieżki legacy. `PluginManifest` nigdy nie dziedziczy po `IAuthKitPlugin` — wspólna semantyka, osobne modele.

### Design Rationale

- Czytanie metadanych przed ładowaniem przesuwa błędy (zły manifest, duplikat Id, za nowy plugin, wyłączony plugin) przed ładowanie assembly — tam są tanie i diagnozowalne.
- Nieprzezroczysta `Location` uniezależnia kontrakt od źródła (dziś katalog, jutro feed albo pakiet) bez przeciekania detali loadera do discovery.
- Atrybucja per kandydat sprawia, że każdy wynik da się wyjaśnić w `PluginLoadResult`, zamiast kłaść cały batch albo crashować start (poprzednie zachowanie przy naruszeniu kontraktu).
- Loader jest celowo głupi: polityka kompatybilności żyje w dokładnie jednym miejscu (bramka + pipeline), więc własne loadery nie zmienią po cichu reguł akceptacji.

## Rejected

- Monolityczny statyczny loader: brak szwu na własne discovery/ładowanie, nietestowalne etapy, późne błędy.
- Tryb warn-only: ładowanie pluginu pod nowszego hosta psuje zachowanie zamiast graceful degradation; silniki polityk (`Strict`/`Warn`/`Ignore`) to przyszła funkcja hosta, nie podstawowa bramka.
- Sufit max-host-version i negocjacja capabilities teraz: zarezerwowane na przyszłe reguły bramki.
- Sprzężenie manifestu z instancją przez dziedziczenie: wiąże model pre-aktywacyjny z konstrukcją runtime, niwecząc sens bramki.

## Consequences

- Każde rozwiązanie pluginu musi dostarczać swój `manifest.json` (commitowany jak Shield i Example albo generowany przy buildzie jak DevTokens i DevTools przez `AuthKit.ManifestGenerator`, który Dockerfile odpala po publikacji) — bez niego plugin odpada na starcie.
- Każde rozwiązanie pluginu powinno commitować lub generować swój `manifest.json` (Shield i Example mają; DevTokens/DevTools generują przy buildzie) — inaczej bramka go nie widzi.
- `SemanticVersion` (SemVer 2.0.0, build metadata ignorowane w precedencji) to jedyne porównywanie wersji; `System.Version` nigdy.
- Przyszłe reguły bramki (capabilities, platforma, max version) wpina się w `CompatibilityGate` bez ruszania discovery ani ładowania.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - discovery pluginów i granica kontraktu
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - ładowanie pluginów i legacy slot middleware
- [ADR-028](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/) - kontrakt pluginu i architektura dynamicznego ładowania

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/030-plugin-middleware-pipeline/) | [Następny]()
