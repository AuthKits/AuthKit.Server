# Marten + PostgreSQL

Marten to dokumentowo-eventowy magazyn PostgreSQL stojący za trwałością AuthKit: magazynem kluczy, powiązaniami tokenów, dokumentami wtyczek i outboxem wiadomości. Jeden Postgres, wiele ról konfigurowany w jednym miejscu.

## Co tam mieszka

- **Magazyn kluczy** zaszyfrowany singleton z materiałem kluczy podpisujących (zobacz [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/))
- **Powiązania tokenów** powiązania kluczy tokenów deweloperskich (zobacz [ADR-012](/pl/adr/012-token-key-bindings-persisted-in-marten/))
- **Dokumenty wtyczek** każda wtyczka implementująca `IMartenPlugin` wnosi własne dokumenty
- **Outbox** magazyn wiadomości Wolverine dzielony z handlerami (`IntegrateWithWolverine`).

Wszystko to zwykłe dokumenty JSONB w jednym klastrze do podglądu wystarczy plain klient Postgres, bez narzędzi Marten.

## Połączenie i schemat

```json
{
  "ConnectionStrings": {
    "Marten": "Host=authdev-db;Port=5432;Database=AuthDev;Username=postgres;Password=postgres"
  }
}
```

Obiekty schematu tworzą się same przy starcie (`AutoCreate.All`) w dev nie piszesz migracji ręcznie.

:::warning[AutoCreate na produkcji]
Automatyczne tworzenie schematu to wygoda deweloperska. Produkcyjnie generuj skrypty migracji z wyprzedzeniem i aplikuj je kontrolowanie zamiast pozwalać aplikacji modyfikować schemat przy starcie.
:::

## Dokumenty wtyczek

Wtyczka wnosi dokumenty jednym hakiem:

```csharp
public interface IMartenPlugin
{
    void ConfigureMarten(StoreOptions options);
}
```

Zasady są twarde i jest ich mało:

- hak wołany jest **raz**, podczas budowania pojedynczego document store hosta, zanim store zostanie otwarty
- wtyczki przetwarzane są w rosnącym porządku id kolejność jest deterministyczna i nie zależy od kolejności discovery
- hak konfiguruje **tylko** `StoreOptions` nie wolno w nim sięgać po `IDocumentStore` ani `IDocumentSession`, te powstają dopiero po zbudowaniu providera
- usługi wtyczki z `ConfigureServices` są już zarejestrowane, kiedy hak leci możesz zakładać ich obecność, ale nie sesje.

## Sesje i outbox

Marten pracuje na lekkich sesjach (`UseLightweightSessions`) zintegrowanych z Wolverine: handler zapisuje dokumenty i publikuje wiadomości w tej samej jednostce pracy. Jeśli handler padnie, wycofuje się i zapis, i wysyłka nie ma stanów „zapisane, ale nie wysłane".

W drugą stronę działa to tak samo: wiadomość w outboxie bez odpowiadającego zapisu nie istnieje. Spójność między „co system zapamiętał" a „co system wysłał" nie jest umową dżentelmeńską, tylko własnością transakcji.

## Operacje

- **Backup** backupuj bazę `AuthDev` (klucze + tokeny + outbox to jeden spójny zestaw) utrata magazynu kluczy unieważnia wszystkie wydane tokeny.
- **Świeży start** `docker compose down -v` kasuje wszystko łącznie ze schematem następny start odbudowuje od zera.
- **Podgląd** zwykły klient Postgres wystarczy: dokumenty to JSONB, czytelne bez narzędzi Marten.

## Diagnostyka

<Expansion title="Brak połączenia">

Sprawdź connection string `Marten` i to, czy `authdev-db` jest `healthy` (`docker compose ps`) host startuje dopiero po zdrowej bazie.

</Expansion>

<Expansion title="Błąd schematu przy starcie">

Ktoś zmienił dokument bez migracji w dev usuń volume (`down -v`), produkcyjnie aplikuj wygenerowany skrypt.

</Expansion>

<Expansion title="Podejrzenie wycieku sesji">

Sesje są lightweight i krótkie długa transakcja zwykle znaczy handler robiący za dużo podziel go na wiadomości.

</Expansion>

<Expansion title="Wiadomości giną przy restarcie">

W trybie `MediatorOnly` (`SkipStorageMigrationOnStartup: true`) nie ma magazynu wiadomości wszystko w locie przepada przy restarcie. Jeśli gubisz wywołania lokalnie, sprawdź najpierw tę flagę, zanim zaczniesz debugować handlery.

</Expansion>

<Expansion title="Tokeny padają po odtworzeniu bazy">

Świeży volume to nowy magazyn kluczy wcześniej wydane tokeny nie mają czym się zweryfikować. To nie błąd, tylko własność rotacji kluczy: wydaj tokeny od nowa albo odtwórz bazę z backupu.

</Expansion>

<Expansion title="Wolne zapytania po dokumentach">

Dokumenty to JSONB filtrowanie po polach bez indeksów boli wraz ze wzrostem. Indeksy dokładasz w tym samym `ConfigureMarten`, w którym definiujesz dokumenty mierz na prawdziwych danych, nie na pustej dev-bazie.

</Expansion>

Zobacz [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/).
