# Wolverine

Wolverine obsługuje komendy i zapytania wewnątrz hosta. Działa jako mediator w procesie: zamiast wołać się bezpośrednio, kod wysyła wiadomości, a Wolverine kieruje je do handlerów z walidacją, trwałością i logowaniem nakładanymi jednolicie wokół każdego wykonania.

## Mediator, nie broker

Nie ma tu zewnętrznego transportu ani kolejek do konfigurowania: wiadomości nie opuszczają procesu. Wolverine to reżyser wywołań w pamięci z jednolitym potokiem (middleware) wokół każdego handlera. Dostajesz porządek architektury (komendy/zapytania zamiast plątaniny wywołań) bez operacyjnej ceny brokera.

## Odkrywanie

Handlery są odkrywane, nie rejestrowane. Przy starcie host włącza dokładnie dwa źródła:

- **assembly Core** (zakotwiczone w `JwtKeyStore`) komendy i zapytania domenowe
- **assembly każdej załadowanej wtyczki** wtyczka wiezie handlery z własnym kodem, a host nigdy nie referencjonuje assembly wtyczek wprost.

Konsekwencja: dodanie handlera to dodanie klasy we właściwym assembly. Nie ma centralnego rejestru do zaktualizowania i zapomnienia.

## Pisanie handlerów

Typy komend/zapytań i handlery lądują w Core (zachowanie domenowe) albo w assembly wtyczki (zachowanie wtyczki). Każdą komendę paruj z walidatorem DevTokens trzyma je w `UseCase/Commands/Validations/` obok `UseCase/Commands/Requests/`.

## Walidacja

FluentValidation działa jako middleware Wolverine, więc niepoprawne wiadomości odpadają przed handlerami. Walidacja nie jest wołana ręcznie w każdym handlerze jest własnością potoku, więc nie da się jej zapomnieć.

## Trwałość i outbox

```json
{
  "AuthKit": {
    "SkipStorageMigrationOnStartup": false
  }
}
```

- Domyślnie pełna trwałość: magazyn wiadomości budowany jest przy starcie, a Marten integruje się jako outbox (`IntegrateWithWolverine`) handlery i persystencja dzielą transakcyjny outbox.
- Z `SkipStorageMigrationOnStartup: true` Wolverine spada do trybu `MediatorOnly` (in-memory, bez magazynu wiadomości) i pomija migrację storage. Używaj do szybkich lokalnych iteracji, gdzie utrata wiadomości w locie przy restarcie nie ma znaczenia.

## Wtyczki wnoszą handlery

To kluczowa własność modelu: logika wtyczki nie kończy się na endpointach. Wtyczka może definiować własne komendy, zapytania i handlery we własnym assembly host włącza je do discovery tak samo jak Core. Endpoint wtyczki zwykle tylko tłumaczy HTTP/gRPC na wiadomość i wysyła ją do Wolverine cała logika żyje w handlerach.

## Logowanie i obserwowalność

Potok domyślnie milczy: poziomy wykonania/sukcesu wiadomości oraz kategorie `Wolverine`, `Marten` i `Npgsql` ustawione są na `None`.

:::tip[Debugowanie potoku]
Podnieś te kategorie tymczasowo, kiedy handler zachowuje się dziwnie cichy potok ukrywa zarówno przepływ wiadomości, jak i SQL za nim.
:::

## Konfiguracja

| Opcja | Domyślnie | Znaczenie |
|-------|------------|-----------|
| `AuthKit:SkipStorageMigrationOnStartup` | `false` | `true` → tryb `MediatorOnly`, brak migracji storage |

Zobacz [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/).
