[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/009-dynamic-plugin-discovery/) | [Następny](/pl/adr/011-keystore-persisted-as-singleton-marten-document/)

# [ADR-010] Ładowanie wtyczek z konfigurowalnego katalogu podczas startu

*2026-08* | Status: accepted

**Tag:** #adr_010

**Date:** 2026-08-26

**Scope:** Host.Plugins

## Kontekst

Host musi uruchamiać pakiety wtyczek bez referencji w czasie kompilacji (zob. ADR-009). Krok ładowania musi nastąpić wystarczająco wcześnie, aby infrastruktura konfigurowana później Wolverine, Marten, MVC mogła zobaczyć złożenia wtyczek podczas budowania własnej konfiguracji.

## Problem

Jeśli wtyczki są ładowane po zbudowaniu hosta, ich procedury obsługi, kontrolery i rejestracje DI są niewidoczne dla frameworków, które już przeskanowały złożenia. Host potrzebuje też przewidywalnego, prostego sposobu lokalizowania wejściowego złożenia każdej wtyczki oraz bezpiecznego obsługiwania błędów, gdy folder jest nieprawidłowy.

## Decyzja

`PluginLoader.LoadPlugins` odkrywa wtyczki z konfigurowalnej ścieżki `PluginsPath` (domyślnie `<base>/plugins`) **przed** wywołaniem `WebApplicationBuilder.Build()`. Każda wtyczka znajduje się we własnym podkatalogu, którego nazwa musi odpowiadać jej wejściowemu złożeniu (`<name>.dll`); program ładujący odnajduje przez refleksję publiczne, nieabstrakcyjne implementacje `IAuthKitPlugin` z konstruktorem bezparametrowym, tworzy ich instancje i zapisuje je jako `LoadedPlugin` (kontrakt + złożenie + katalog). Złożenia są ładowane do `AssemblyLoadContext.Default` (a nie do izolowanego kontekstu), dzięki czemu typy frameworka i pakietów są współdzielone ponad granicą, a gorące przeładowywanie jest jawnie niewspierane.

### PluginLoader

**Obowiązki:**

- Wyliczanie podkatalogów wtyczek i rozwiązywanie zależności każdego wejściowego złożenia.
- Walidowanie i tworzenie instancji implementacji `IAuthKitPlugin`.
- Zwracanie wyłącznie pomyślnie załadowanych wtyczek, logowanie i pomijanie nieprawidłowych folderów.

### LoadedPlugin

**Obowiązki:**

- Przenoszenie instancji kontraktu, złożenia i katalogu źródłowego do późniejszego użycia przez hosta (DI, odkrywanie złożeń przez Wolverine/MVC).

### Uzasadnienie projektowe

- Ładowanie przed `Build` pozwala Wolverine, Marten i MVC odkrywać typy wtyczek podczas konfiguracji.
- Domyślny kontekst ładowania współdzieli typy uruchomieniowe (Wolverine `IMessageBus`, Marten `IDocumentSession`, typy MVC) ponad granicą, unikając błędów tożsamości typów.
- Konwencja nazewnictwa oraz łagodne pomijanie sprawiają, że wdrożenie jest proste i odporne na uszkodzony folder wtyczki.

## Odrzucone

- Ładowanie wtyczek do izolowanych `AssemblyLoadContext` (niezgodności tożsamości typów ze współdzielonymi frameworkami).
- Ładowanie po zbudowaniu hosta (infrastruktura nie widziałaby złożeń wtyczek).
- Wspieranie gorącej podmiany lub wyładowywania wtyczek w czasie działania.
- Wymaganie pliku manifestu wykraczającego poza konwencję nazewnictwa katalogu/złożenia.

## Konsekwencje

Wtyczki są dostępne przed skonfigurowaniem infrastruktury i uczestniczą jednolicie w DI, przesyłaniu komunikatów i MVC. Kosztem jest brak izolacji i przeładowywania w czasie działania, a host ufa złożeniom wtyczek załadowanym do domyślnego kontekstu, więc pochodzenie wtyczek musi być kontrolowane operacyjnie.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - ładowany kontrakt wtyczki
- [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) - załadowane złożenia zasilają Wolverine/Marten

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/009-dynamic-plugin-discovery/) | [Następny](/pl/adr/011-keystore-persisted-as-singleton-marten-document/)
