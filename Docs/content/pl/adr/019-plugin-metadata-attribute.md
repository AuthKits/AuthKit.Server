[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/018-security-scheme-contract-explicit-handling/) | [Następny](/pl/adr/020-devtools-plugin/)

# [ADR-019] Deklarowanie tożsamości wtyczki poprzez atrybut PluginMetadata

*2026-09* | Status: accepted

**Tag:** #adr_019

**Date:** 2026-09-11

**Scope:** AuthKit.Plugins.Abstractions

## Kontekst

Każda implementacja `IAuthKitPlugin` (ADR-009) eksponowała swoją tożsamość poprzez nadpisywanie właściwości `Name`, `Version` oraz `Description` w kodzie. Wtyczki DevTokens i DevTools powielały ten szablon, a bogatsze informacje katalogowe stabilny identyfikator maszynowy, tagi, możliwości, zależności, autor, licencja, URL repozytorium nie miały w ogóle powierzchni kontraktu.

## Problem

Powtarzanie tożsamości jako właściwości kodu C# rozprasza tożsamość wtyczki po treści klasy, zamienia podbicie wersji w edycję kodu i nie pozostawia jednego strukturalnego miejsca, w którym przyszłe narzędzia (diagnostyka ładowania, panel administracyjny) mogłyby odczytać tożsamość i metadane katalogowe. Dodanie metadanych katalogowych wymagałoby nowych elementów kontraktu interfejsu, zmuszając każdą istniejącą wtyczkę do ich implementacji.

## Decyzja

Tożsamość wtyczki i metadane katalogowe przenoszą się do atrybutu `[PluginMetadata]` na klasie wtyczki:

- `PluginMetadataAttribute` (przestrzeń nazw `AuthKit.Plugins.Abstractions.Contracts.Plugins`) deklaruje `Id`, `Version`, `Name`, `Tags`, `DependsOn`, `Capabilities`, `DisplayName`, `Description`, `Author`, `License`, `LicenseUrl`, `Homepage` oraz `RepositoryUrl`. Konstruktor to `(id, version, name, ...)` ze wszystkimi polami po `name` opcjonalnymi.
- `IAuthKitPlugin.Name`, `Version` oraz `Description` są teraz **domyślnymi elementami interfejsu**, które odczytują atrybut poprzez prywatną metodę pomocniczą `GetPluginMetadata()`. Wtyczki nie muszą ich już implementować: `Name` przyjmuje nazwę typu, a `Version` wartość `0.0.0`, gdy klasa nie ma atrybutu.
- Wtyczki mogą nadal nadpisywać te trzy właściwości, jeśli potrzebują wyliczanej tożsamości, lecz atrybut jest preferowanym punktem deklaracji. Obie dostarczone wtyczki (`DevTokens`, `DevTools`) deklarują swoją tożsamość wyłącznie poprzez `[PluginMetadata]`.
- `AttributeUsage` to `Class`, `Inherited = false`, `AllowMultiple = false` jeden atrybut na klasę wtyczki.
- Metadane tablicowe (`Tags`, `DependsOn`, `Capabilities`) są eksponowane jako `IReadOnlyList<string>` i domyślnie przyjmują pustą listę, gdy ich brak, więc konsumenci nigdy nie widzą `null`.

### Uzasadnienie projektowe

- **Jeden punkt deklaracji**: tożsamość i katalogowanie znajdują się w jednym atrybucie, obok opisywanej klasy.
- **Zgodność wsteczna**: domyślne elementy interfejsu utrzymują kompilowalność i ładowalność starych klas wtyczek; żadna zmiana hosta, wtyczki ani `PluginLoader` nie była wymagana dla odkrywania ani wyjścia startowego.
- **Addytywne katalogowanie**: metadane tagów/możliwości/zależności/autorstwa zyskują powierzchnię kontraktu bez dotykania listy elementów interfejsu (bez łamiącej zmiany dla istniejących implementacji).
- **Deklaratywność zamiast imperatywności**: metadane jako atrybut są czytelne, wykrywalne poprzez refleksję i użyteczne przez narzędzia, które nigdy nie instancjonują klasy wtyczki.

## Odrzucone

- Dodanie `Tags`, `Capabilities`, `Dependencies` itd. jako nowych abstrakcyjnych elementów `IAuthKitPlugin` łamie każdą istniejącą implementację wtyczki (łamanie kontraktu w czasie kompilacji).
- Zachowanie tożsamości wyłącznie jako właściwości kodu obok osobnego atrybutu katalogowego rozdziela tożsamość na dwa mechanizmy.
- Plik metadanych (JSON/zasób osadzony) jako dodatek traci sprawdzanie w czasie kompilacji i wykrywalność przez refleksję przy niewielkim zysku.

## Konsekwencje

Nowe wtyczki deklarują jeden atrybut i otrzymują poprawne `Name`/`Version`/`Description` za darmo; program ładujący wtyczki hosta konsumuje atrybut poprzez domyślne elementy interfejsu, więc wyjście startowe `ServerHost` ("Loaded plugin 'DevTools' v1.0.0") odzwierciedla wartości atrybutu. Pola katalogowe są dostępne dla przyszłych powierzchni administracyjnych bez dalszych zmian kontraktu. Ponieważ domyślne elementy interfejsu można nadpisywać, wtyczka potrzebująca wyliczanej tożsamości zachowuje tę swobodę, lecz obie dostarczone wtyczki już z niej nie korzystają.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - kontrakt `IAuthKitPlugin` rozszerzany przez ten ADR
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - program ładujący odczytujący tożsamość wtyczki przy starcie

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/018-security-scheme-contract-explicit-handling/) | [Następny](/pl/adr/020-devtools-plugin/)
