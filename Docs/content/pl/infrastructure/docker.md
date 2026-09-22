# Docker + Kestrel

Cały stos jedzie z jednego pliku compose: AuthKit, PostgreSQL i Keycloak. Ta strona prowadzi przez pełny cykl pierwszy start, codzienność, debugowanie i to, co zmienia się w drodze na produkcję.

## Wymagania

- [Docker](https://docs.docker.com/get-docker/) z Compose v2.
- Wolne porty `5000`, `5001`, `8081`, `5433`, `5434`.
- Deweloperski certyfikat HTTPS w `./certs` (zobacz [Certyfikaty](#certyfikaty)).

## Pierwszy start

```bash
cp .env.example .env
docker compose up --build
```

Co dzieje się po kolei:

1. Budują się obrazy (`auth` z rootowego `Dockerfile`, `keycloak` z `Deploy/Keycloak/Dockerfile`) bazy ciągną `postgres:15`.
2. Powstają nazwane volumy `AuthDev-data` i `Auth-data` dane przeżywają restarty kontenerów.
3. `authdev-db` musi zgłosić `healthy` (`pg_isready`), zanim `auth` w ogóle ruszy.
4. `auth` robi migrację storage, mapuje endpointy REST + gRPC i zaczyna słuchać.

Zweryfikuj endpointem health:

```bash
curl -k https://localhost:5000/health
```

## Usługi

| Usługa | Obraz / Build | Porty | Cel |
|--------|---------------|-------|-----|
| `auth` | `Dockerfile` (root repo) | `5000` REST, `5001` gRPC | Host AuthKit (HTTPS) |
| `authdev-db` | `postgres:15` | `5434` → `5432` | Baza AuthKit (volume `AuthDev-data`) |
| `keycloak` | `Deploy/Keycloak/Dockerfile` | `8081` → `8080` | Dostawca tożsamości, realm auto-importowany |
| `keycloak-db` | `postgres:15` | `5433` → `5432` | Baza Keycloak (volume `Auth-data`) |

`auth` montuje `./certs` na deweloperski certyfikat HTTPS i czyta `.env` plus `ConnectionStrings__DefaultConnection` wskazujący na `authdev-db`. Wszystko gada po sieci mostkowej `db_net` serwisy widzą się po nazwach (`authdev-db`, `keycloak-db`).

## Profil Keycloak

`keycloak` i `keycloak-db` siedzą za profilem `keycloak` w compose: bazowy `up` stawia AuthKit z własną bazą, a `docker compose --profile keycloak up` dokłada stos tożsamości. Plik realm jest montowany read-only i importowany przy starcie (`--import-realm`) bootstrap admin to `admin` / `admin`.

Po pierwszym starcie otwórz http://localhost:8081 i potwierdź, że realm `authz` istnieje. Credentiale klienta biorą się z `.env` (`KEYCLOAK_CLIENT_ID`, `KEYCLOAK_CLIENT_SECRET`).

## Zmienne środowiskowe

Kopia tego, co deklaruje `.env.example` ustaw przed pierwszym `up`:

| Zmienna | Znaczenie |
|---------|-----------|
| `DEV_CERT_PATH` / `DEV_CERT_PASSWORD` | Deweloperski certyfikat HTTPS i hasło do niego |
| `DEV_CERT_PORT_REST` / `DEV_CERT_PORT_GRPC` | Porty pokrywane certyfikatem (`5000` / `5001`) |
| `KEYCLOAK_URL` / `KEYCLOAK_REALM` | Gdzie host znajduje Keycloak i który realm |
| `KEYCLOAK_CLIENT_ID` / `KEYCLOAK_CLIENT_SECRET` | Credentiale klienta (sekret wymagany) |
| `JWT__KEY` / `JWT__ISSUER` / `JWT__AUDIENCE` | Podpisywanie i walidacja tokenów deweloperskich |
| `ERRORMETADATA__DOCSBASEURL` | Bazowy URL renderowany w linkach metadanych błędów |

Minimum: zmień `DEV_CERT_PASSWORD` i `KEYCLOAK_CLIENT_SECRET` przykładowe wartości są publiczne.

## Certyfikaty

HTTPS terminuje na dev-cercie z `./certs` (`DEV_CERT_PATH`, `DEV_CERT_PASSWORD` w `.env`). Podmontuj tam własny `.pfx` dla zaufanego lokalnego łańcucha prawdziwego hasła nigdy nie commituj. `curl -k` w przykładach pomija weryfikację dokładnie dlatego, że to dev-cert.

:::warning[Nigdy nie commituj sekretów]
`.env` trzyma hasła i klucze. Jest w gitignore nie bez powodu wyciek `DEV_CERT_PASSWORD` albo `KEYCLOAK_CLIENT_SECRET` znaczy rotację credentiali wszędzie.
:::

## Health i logi

`auth` niesie healthcheck (`curl -k https://localhost:5000/health`, co 30s, 3 próby) bazy używają `pg_isready` (co 5s). Przydatne komendy:

```bash
docker compose ps            # cokolwiek nie healthy/running pada szybko
docker compose logs -f auth  # output hosta (Wolverine/Marten/Npgsql domyślnie wyciszone do None)
docker compose logs -f authdev-db
```

Jeśli `auth` nigdy nie robi się healthy, sprawdź najpierw bazę niezdrowa `authdev-db` blokuje hosta z założenia (warunek `service_healthy`).

## Przebudowa i sprzątanie

```bash
docker compose up --build        # przebuduj po zmianach w kodzie
docker compose down              # stop, volumy zostają (dane zostają)
docker compose down -v            # stop I wywalenie volumów (świeża baza)
```

`down -v` użyj, kiedy chcesz odtworzyć migrację storage od zera albo wyrzucić zepsutą dev-bazę.

:::danger[down -v niszczy dane]
Wywalenie volumów kasuje szyfrowany magazyn kluczy wcześniej wydane tokeny przestają się weryfikować i trzeba je wydać od nowa. Nie ma cofnięcia.
:::

## Rozwiązywanie problemów

- **Port zajęty** inny Postgres na `5434`/`5433` albo coś na `5000`/`8081`: zatrzymaj albo przemapuj lewą stronę wpisu `ports`.
- **Brak realm w Keycloak** profil nie włączony (`--profile keycloak`) albo plik realm nie zamontowany sprawdź, czy `Deploy/Keycloak/realms/realm-authz.json` istnieje.
- **`auth` wychodzi od razu** zwykle brak `.env` albo złe `DEV_CERT_PASSWORD`, więc Kestrel nie ładuje certyfikatu czytaj `docker compose logs auth`.
- **Wołania tokenami padają po restarcie** ze świeżym volumem magazyn kluczy jest nowy, więc wcześniej wydane tokeny już się nie weryfikują. Oczekiwane: rotuj albo wydaj od nowa.

## Bez Dockera

Host to zwykły ASP.NET Core na Kestrelu:

```bash
dotnet restore AuthKit.slnx
dotnet build AuthKit.slnx --configuration Release
dotnet run --project src/Host/Host.csproj
```

Domyślny adres: `http://0.0.0.0:8080` (HTTP/2). PostgreSQL i Keycloak muszą być i tak osiągalne zobacz [Szybki start](/pl/guide/quick-start/#option-2-run-locally).

## W stronę produkcji

- Prawdziwe sekrety trzymaj w vaulcie albo secret store platformy, nigdy w plikach `.env`.
- Terminuj TLS na brzegu (reverse proxy / ingress) z prawidłowym certyfikatem zamiast dev-`.pfx`.
- Backupuj volume `AuthDev-data` leży tam szyfrowany magazyn kluczy i powiązania tokenów.
- Przypnij tagi obrazów (`postgres:15` pływa) i ustaw jawne limity zasobów na serwis.
