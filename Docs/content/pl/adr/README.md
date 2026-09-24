# Rejestr Decyzji Architektonicznych

Ten katalog przechowuje decyzje architektoniczne AuthKit.Server w formie ADR. Nie jest to dziennik zmian ani przepisana historia commitów. Jest to wyselekcjonowany zbiór decyzji technicznych, który wyjaśnia, dlaczego system ma obecny kształt oraz jakie ograniczenia nakłada to na przyszłe prace.

Rekordy ADR są pogrupowane według obszarów architektury. Numeracja jest globalna, więc identyfikator decyzji nie zależy od pliku, w którym się znajduje. Przeniesienie pliku do innego obszaru nie zmienia jego numeru, a numer należy traktować jako stabilny identyfikator decyzji.

## Jak czytać ten zbiór

Zacznij od obszaru odpowiadającego temu, co zmieniasz:

- jeśli zmiana dotyczy zarządzania kluczami, powiązań kluczy tokenów lub kontraktów podstawowej domeny, przeczytaj poniższe decyzje `AuthKit.Core`
- jeśli dotyczy powierzchni REST lub gRPC, CLI, konfiguracji albo sposobu składania i hostowania serwera, zobacz decyzje Host
- jeśli dotyczy systemu wtyczek, rozwiązań lub abstrakcji wtyczek, zobacz decyzje Plugins

Każdy ADR zawiera:

- `Context`:
  warstwę i architektoniczne umiejscowienie decyzji
- `Problem`:
  konkretne napięcie techniczne, które decyzja rozwiązuje
- `Decision`:
  wybrany kierunek i granicę odpowiedzialności
- `Rejected`:
  realistyczne alternatywy, które celowo nie zostały wybrane
- `Consequences`:
  wpływ na utrzymanie, ograniczenia, efekty uboczne oraz praktyczne konsekwencje dla przyszłego kodu

## Jak używać ADR-ów podczas zmian

ADR-y nie zastępują czytania kodu, ale obniżają koszt zrozumienia decyzji, które zostały już podjęte. Gdy zmieniasz dany obszar:

1. znajdź pasujący obszar
2. przeczytaj 2–4 najbliższe ADR-y, nie tylko jeden
3. sprawdź, czy nowa zmiana rozszerza bieżący model, czy faktycznie go łamie
4. jeśli decyzja nie jest już prawdziwa, dodaj nowy ADR zamiast po cichu odchodzić od bieżącego kierunku

Ten zbiór ma zachować spójność pomiędzy Core, Host i Plugins. W AuthKit.Server znaczna część kosztu zmian wynika z kontraktów pomiędzy warstwami, a nie z pojedynczej klasy.

## Mapa kategorii

Poniższa tabela przedstawia obszary architektury i ich bieżący zakres.

| Area | Scope |
| --- | --- |
| Core | Współdzielony model domeny: zarządzanie kluczami podpisującymi, powiązania kluczy tokenów, kontrakty błędów oraz interfejsy Core. |
| Host | Kompozycja aplikacji i powierzchnia zewnętrzna: REST, gRPC, CLI, konfiguracja oraz hosting materiału kluczy. |
| Plugins | System wtyczek: rozwiązania, abstrakcje wtyczek oraz granice rozszerzeń. |

## Bieżące ADR-y

| ID | Title | Area | Status | Date |
|----|-------|------|--------|------|
| [ADR-001](/pl/adr/001-centralize-signing-key-management/) | Scentralizuj zarządzanie kluczami podpisującymi poprzez abstrakcję głównego magazynu kluczy w Core | Core | accepted | 2026-08-26 |
| [ADR-002](/pl/adr/002-encrypt-keystore-at-rest/) | Szyfruj utrwalony materiał magazynu kluczy w spoczynku poprzez wymienialny szyfrator | Core | accepted | 2026-08-26 |
| [ADR-003](/pl/adr/003-signing-key-lifecycle-immutable-transitions/) | Modeluj cykl życia klucza podpisującego jako niezmienne przejścia stanu | Core | accepted | 2026-08-26 |
| [ADR-004](/pl/adr/004-token-key-bindings-domain/) | Traktuj powiązania tokenów deweloperskich z kluczami podpisującymi jako domenę Core | Core | accepted | 2026-08-26 |
| [ADR-005](/pl/adr/005-public-keys-via-jwks/) | Publikuj klucze publiczne poprzez JWKS, ujawniając wyłącznie klucze nieunieważnione | Core | accepted | 2026-08-26 |
| [ADR-006](/pl/adr/006-kid-as-generated-guid/) | Wyznaczaj identyfikator klucza JWT jako generowany GUID | Core | accepted | 2026-08-26 |
| [ADR-007](/pl/adr/007-default-signing-algorithm-rsa-4096/) | Domyślnym algorytmem podpisywania jest RSA-4096 z RS256 | Core | accepted | 2026-08-26 |
| [ADR-008](/pl/adr/008-standardized-error-response/) | Standaryzuj błędy API poprzez kontrakt odpowiedzi na błąd w Core | Core | accepted | 2026-08-26 |
| [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) | Odkrywaj i ładuj wtyczki dynamicznie poprzez kontrakt IAuthKitPlugin | Plugins | accepted | 2026-08-26 |
| [ADR-010](/pl/adr/010-plugin-loading-from-directory/) | Ładuj wtyczki z konfigurowalnego katalogu podczas startu | Host | accepted | 2026-08-26 |
| [ADR-011](/pl/adr/011-keystore-persisted-as-singleton-marten-document/) | Utrwalaj zaszyfrowany magazyn kluczy jako dokument singleton w Marten | Host | accepted | 2026-08-26 |
| [ADR-012](/pl/adr/012-token-key-bindings-persisted-in-marten/) | Utrwalaj powiązania kluczy tokenów w Marten | Host | accepted | 2026-08-26 |
| [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) | Udostępniaj powierzchnie transportowe REST i gRPC | Host | accepted | 2026-08-26 |
| [ADR-014](/pl/adr/014-error-responses-via-middleware/) | Renderuj błędy HTTP jako szczegóły problemu RFC 7807 poprzez oprogramowanie pośredniczące | Host | accepted | 2026-08-26 |
| [ADR-015](/pl/adr/015-keycloak-external-jwt-authority/) | Używaj Keycloak jako zewnętrznego urzędu JWT | Host | accepted | 2026-08-26 |
| [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) | Używaj Marten i Wolverine jako infrastruktury hosta | Host | accepted | 2026-08-26 |
| [ADR-017](/pl/adr/017-api-key-credential-extraction-strategies/) | Zdefiniuj strategie ekstrakcji poświadczeń klucza API | Host | accepted | 2026-09-11 |
| [ADR-018](/pl/adr/018-security-scheme-contract-explicit-handling/) | Obsługuj wartości kontraktu schematu bezpieczeństwa wprost | Plugins | accepted | 2026-09-11 |
| [ADR-019](/pl/adr/019-plugin-metadata-attribute/) | Deklaruj tożsamość wtyczki poprzez atrybut PluginMetadata | Plugins | accepted | 2026-09-11 |
| [ADR-020](/pl/adr/020-devtools-plugin/) | Hostuj narzędzia deweloperskie poprzez dedykowaną wtyczkę DevTools | Plugins | accepted | 2026-09-11 |
| [ADR-021](/pl/adr/021-swagger-serving-via-reflection/) | Serwuj Swagger poprzez oparty na refleksji SwaggerHost w DevTools | Plugins | accepted | 2026-09-11 |
| [ADR-022](/pl/adr/022-plugin-configuration-context-and-builder/) | Rozszerz konfigurację wtyczek o builder hosta i zakresowy kontekst | Plugins | accepted | 2026-09-12 |
| [ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/) | Integruj punkty końcowe i oprogramowanie pośredniczące wtyczek poprzez jawne haki potoku hosta | Plugins | accepted | 2026-09-12 |
| [ADR-024](/pl/adr/024-plugin-lifecycle-and-hosted-services/) | Połącz haki cyklu życia wtyczek ze standardowym cyklem życia hosta .NET | Plugins | accepted | 2026-09-12 |
| [ADR-025](/pl/adr/025-plugin-options-openapi-and-marten-integrations/) | Utrzymuj integracje opcji wtyczek, OpenAPI i Marten wprost | Plugins | accepted | 2026-09-12 |
| [ADR-026](/pl/adr/026-plugin-authentication-and-authorization-hooks/) | Konfiguruj uwierzytelnianie i autoryzację wtyczek poprzez infrastrukturę bezpieczeństwa hosta | Plugins | accepted | 2026-09-12 |
| [ADR-027](/pl/adr/027-devtools-ui-typescript/) | Kompiluj interfejs DevTools z modularnego TypeScript do pojedynczego zasobu osadzonego | Plugins | accepted | 2026-09-16 |
| [ADR-028](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/) | Zdefiniuj kontrakt wtyczek i architekturę dynamicznego ładowania | Plugins | accepted | 2026-09-11 |
| [ADR-029](/pl/adr/029-structured-plugin-health-contract/) | Eksponowanie ustrukturyzowanych i anulowalnych wyników kondycji wtyczek | Plugins | accepted | 2026-09-13 |
| [ADR-030](/pl/adr/030-plugin-middleware-pipeline/) | Deklaratywny Pipeline Middleware Pluginów Z Jawnym Transportem | Plugins | accepted | 2026-09-24 |

## Relacje pomiędzy obszarami

Najczęstszy przepływ architektoniczny w AuthKit.Server wygląda tak:

`Core -> Host -> Plugins`

Nie jest to ścisły diagram zależności projektu, ale użyteczna mapa do czytania decyzji. W praktyce:

- `Core` definiuje, co system uznaje za dane domenowe oraz kontrakty kryptograficzne (klucze, powiązania, błędy)
- `Host` definiuje, jak ta domena jest udostępniana i składana w działający serwer (powierzchnie API, CLI, konfiguracja)
- `Plugins` rozszerzają zachowanie na bazie stabilnych kontraktów Core i Host

## Kiedy dodać nowy ADR

Nowy ADR warto dodać, gdy zmiana:

- przesuwa granice odpowiedzialności pomiędzy warstwami
- wprowadza nowy kontrakt danych lub nowy trwały artefakt
- zmienia model wykonywania zarządzania kluczami, hostingu lub ładowania wtyczek
- dodaje nowe zachowanie dostawcy lub wtyczki, które nie mieści się już w bieżącym modelu
- zastępuje wcześniejszą decyzję innym świadomym kompromisem

ADR-u zwykle nie warto dodawać dla:

- zwykłego refaktoryzowania bez architektonicznej zmiany kierunku
- kosmetycznej lokalnej zmiany API ograniczonej do jednego pliku lub klasy
- zmiany wyłącznie dokumentacyjnej lub testowej, która nie zmienia kontraktów systemu
