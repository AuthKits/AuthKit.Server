# Szybki start

Uruchom AuthKit w kilka minut — od zera do zweryfikowanego tokenu. Jeśli wolisz najpierw zrozumieć całość, zacznij od [Wprowadzenia](/pl/guide/introduction/).

## Wymagania wstępne

- Zainstalowany [Docker](https://docs.docker.com/get-docker/) (zalecana ścieżka).
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — tylko do rozwoju lokalnego bez Dockera.
- Wolne porty: `5000`, `5001`, `8081`, `5433`, `5434`.

## Krok 0: Plik .env

Cała konfiguracja sekretów żyje w `.env`, którego nigdy nie commitujesz:

```bash
cp .env.example .env
```

Minimum do zmiany przed pierwszym startem:

- `DEV_CERT_PASSWORD` — hasło do deweloperskiego certyfikatu HTTPS;
- `KEYCLOAK_CLIENT_SECRET` — sekret klienta Keycloak.

Reszta działa na defaultach. Pełna mapa zmiennych: [Docker + Kestrel](/pl/infrastructure/docker/).

## Opcja 1: Uruchomienie przez Docker (zalecane)

```bash
docker compose up --build
```

Stawia AuthKit wraz z PostgreSQL (Keycloak dokłada profil `keycloak`). Pierwszy start buduje obrazy i robi migrację storage — potrwa chwilę, kolejne starty są szybkie.

### Porty

| Usługa | Port | Przeznaczenie |
|--------|------|---------------|
| AuthKit REST | `5000` | REST API (HTTPS) |
| AuthKit gRPC | `5001` | gRPC API (HTTPS) |
| Keycloak | `8081` | Dostawca tożsamości (HTTP) |
| PostgreSQL (AuthKit) | `5434` | Baza danych AuthKit |

### Konfiguracja Keycloak

1. Otwórz http://localhost:8081
2. Zaloguj się admin/admin
3. Zaimportuj realm z `Deploy/Keycloak/realms/realm-authz.json`
4. Potwierdź, że realm `authz` istnieje i ma klienta `workspace-authz`

Bez zaimportowanego realm logowanie deweloperów nie zadziała — to najczęstszy powód 401 na świeżym stosie.

## Opcja 2: Uruchomienie lokalne

Do rozwoju bez Dockera:

```bash
# Przywróć zależności
dotnet restore AuthKit.slnx

# Zbuduj
dotnet build AuthKit.slnx --configuration Release

# Uruchom hosta
dotnet run --project src/Host/Host.csproj
```

Domyślny adres nasłuchu: `http://0.0.0.0:8080` (HTTP/2). PostgreSQL i Keycloak muszą być osiągalne osobno — najprościej postawić same bazy z compose, a hosta lokalnie.

Do iteracji nad kodem użyj watchera:

```bash
dotnet watch run --project src/Host/Host.csproj
```

## Sprawdź, czy wszystko działa

Sprawdź endpoint health:

```bash
curl -k https://localhost:5000/health
```

Oczekiwana odpowiedź:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234560",
  "componentDurations": {
    "AuthKitPlugins": "Healthy",
    "Marten": "Healthy"
  }
}
```

Zerknij też na `docker compose ps` — każdy serwis ma być `healthy` albo `running`.

## Pierwsza sesja end-to-end

Skoro health jest zielony, zrób pełne kółko w 5 minut:

1. Otwórz Swagger na działającym hoście: `https://localhost:5000/devtools/swagger`.
2. Zaloguj się do Keycloak (http://localhost:8081, `admin` / `admin`) i skopiuj swój token dostępowy.
3. W Swaggerze wywołaj `POST /sdk/developer-tokens` z tokenem Keycloak w `Authorize` — dostaniesz JWT deweloperski plus klucz `rk_live_…`.
4. Wywołaj `POST /sdk/tokens/verify` z tym JWT — odpowiedź `"valid": true` zamyka pętlę.

Dopiero teraz masz działający łańcuch: Keycloak → AuthKit → token SDK → weryfikacja.

## Zatrzymywanie i czyszczenie

```bash
docker compose stop          # stop, wszystko zostaje
docker compose down          # stop, kontenery precz, dane zostają
docker compose down -v       # stop + kasowanie volumów (świeża baza, nowe klucze!)
```

:::warning[down -v kasuje klucze]
Po wywaleniu volumów magazyn kluczy jest nowy — wcześniej wydane tokeny przestają się weryfikować.
:::

## Rozwiązywanie problemów

- **Kontenery wstają, ale health nie odpowiada** — poczekaj na `healthy` w `docker compose ps`; `auth` startuje dopiero po zdrowej bazie.
- **Keycloak pusty po starcie** — profil `keycloak` nie włączony (`docker compose --profile keycloak up`) albo brak pliku realm.
- **Błąd certyfikatu przy starcie** — złe `DEV_CERT_PASSWORD` albo brak `./certs`; szczegóły w logach `auth`.
- **401 ze Swaggera** — brak lub zły token Keycloak w `Authorize`; tokeny SDK nie zastępują bearer Keycloak.
- **Port zajęty** — coś siedzi na `5000`/`8081`/`5434`; zatrzymaj albo przemapuj lewą stronę `ports`.
- **Czysty reset** — `docker compose down -v` kasuje bazy i stawia wszystko od zera (tokeny też przepadają).

## Następne kroki

1. [Skonfiguruj Keycloak](/pl/guide/configuration/)
2. [Utwórz token deweloperski](/pl/reference/rest-api/)
3. [Poznaj REST API](/pl/reference/rest-api/)
4. [Docker + Kestrel](/pl/infrastructure/docker/) — pełny cykl życia stosu
