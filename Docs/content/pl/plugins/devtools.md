# DevTools

Kiedy AuthKit działa, nie chcesz zgadywać, co wystawia chcesz to zobaczyć. Wtyczka `DevTools` to wbudowany właz inspekcyjny serwera: hostuje narzędzia deweloperskie w działającym procesie, więc ten sam binarek, który obsługuje klientów SDK, odpowiada też na pytanie „co jest teraz naprawdę zmapowane?".

To pytanie jest trudniejsze, niż brzmi. AuthKit składa swoją powierzchnię przy starcie z hosta i każdej odkrytej wtyczki: endpointy REST, serwisy gRPC, schematy bezpieczeństwa OpenAPI, health checki. Zameldowany plik specyfikacji zgniótłby w tydzień. DevTools omija ten problem, generując wszystko z połączonej aplikacji w runtime to, co przeglądasz, zawsze jest tym, co naprawdę tam jest.

## Co dostajesz

Uruchom AuthKit, a czekają trzy drzwi (wszystkie ścieżki konfigurowalne, zobacz [Opcje](#Opcje)):

<script>
  import IntroCards from '$lib/IntroCards.svelte'

  const doorsCards = [
    { icon: 'terminal', title: '/devtools', desc: 'Osadzony UI DevTools.', href: '#UI-podróżujące-z-wtyczką' },
    { icon: 'code', title: '/devtools/swagger', desc: 'Swagger UI (DocumentName: v1, tytuł AuthKit API).', href: '#Swagger-z-refleksji' },
    { icon: 'sync', title: '/grpc-ui', desc: 'UI eksploracji gRPC.', href: '#Eksploracja-gRPC-bez-klientów' },
  ]
</script>

<IntroCards cards={doorsCards} />

Typowa sesja płynie tak:

1. Startujesz stos, otwierasz `/devtools/swagger` i przechodzisz endpointy tokenów z [REST API Reference](/pl/reference/rest-api/) wydajesz token deweloperski, potem go weryfikujesz, nie pisząc ani linii kodu klienta.
2. Kiedy coś zachowuje się dziwnie, przełączasz się na `/grpc-ui` i wywołujesz bazowe metody `jwks` albo `monitoring` bezpośrednio.
3. Porównujesz oba: jeśli gRPC odpowiada czysto, problem siedzi w transporcie jeśli nie głębiej.

## Swagger z refleksji

Za `/devtools/swagger` stoi `SwaggerHost`. Host i tak generuje dokument OpenAPI przez swój potok generowania (SwaggerGen) rolą wtyczki jest serwowanie tego dokumentu i UI z własnego wnętrza, wniesionych z powrotem do potoku hosta jak każde inne middleware.

Liczą się tu dwa szczegóły. Po pierwsze, dokument obejmuje też endpointy i schematy bezpieczeństwa od wtyczek własna wtyczka deklarująca schemat OpenAPI pojawia się w Swaggerze bez żadnej dodatkowej pracy. Po drugie, oparte na refleksji instancje middleware budowane są raz i używane ponownie przy każdym żądaniu, więc serwowanie dokumentacji nie płaci podatku refleksji na żądanie. Zobacz [ADR-021](/pl/adr/021-swagger-serving-via-reflection/).

:::tip[Za darmo dla Twojej wtyczki]
Jeśli piszesz własną wtyczkę, nic nie musisz robić, żeby trafiła do Swaggera wystarczy, że zadeklaruje schematy przez kontrakt. Szczegóły: [Własne wtyczki](/pl/plugins/custom/).
:::

## Eksploracja gRPC bez klientów

gRPC jest potężne, ale niezręczne w macaniu: zwykle potrzebujesz wygenerowanych stubów w swoim języku, zanim cokolwiek wywołasz. `GrpcServiceCatalog` usuwa to tarcie. Śledzi każdy zmapowany serwis gRPC hostowe `jwks` i `monitoring` oraz wtyczkowe wraz z deskryptorami protobuf i komentarzami dokumentacyjnymi z plików `.proto`. Z tego katalogu wywodzi się dokumentacja [powierzchni gRPC](/pl/reference/grpc/) i to po nim chodzi UI.

`GrpcDynamicInvoker` idzie o krok dalej: realnie wykonuje unarne wywołania odkryte przez katalog. Wiadomości są marszalowane przez deskryptory dostarczone przez serwer, więc nigdzie w pętli nie potrzeba kompilowanego kodu klienta, a wynik wraca jako serializowalny `GrpcInvocationResult` z `Success`, `StatusCode`, `StatusName`, `Detail` i payloadem.

Każde narzędzie ma krawędzie poznaj te trzy, zanim oprzesz się na invokerze:

- **tylko unarne**: metody strumieniowe klient/serwer zgłaszane są jako niewspierane zamiast być wykonane do połowy
- **nieznane metody padają czysto**: zła para `serwis/metoda` zwraca `NotFound` w wyniku zamiast rzucać
- **poluzowany TLS**: walidacja certyfikatów jest poluzowana, bo host jedzie na certyfikacie deweloperskim dokładnie to, czego chcesz lokalnie, i dokładnie to, czego nie chcesz kopiować do produkcyjnych narzędzi.

:::warning[Tylko lokalnie]
Invoker celowo luzuje TLS pod certyfikat deweloperski hosta. Wołanie zdalnych, produkcyjnych serwisów tym samym kanałem mija się z celem — tam używaj pełnej weryfikacji.
:::

## UI podróżujące z wtyczką

Frontend DevTools (`src/Plugins/Solutions/DevTools/UI/`) to modularna aplikacja TypeScript Svelte 5 z Tailwindem kompilowana do pojedynczego osadzonego zasobu. W praktyce znaczy to, że UI podróżuje wewnątrz assembly wtyczki: deploy AuthKit nigdy nie wymaga osobnego kroku plików statycznych, a wersja UI nie może rozjechać się z wersją serwera, który inspekcjonuje.

### Co potrafi UI

- **Drzewo serwisów** sidebar z każdym skatalogowanym serwisem gRPC, przeglądany po pakietach i serwisach
- **Wywoływanie metod** wybierz metodę unarną, wypełnij payload żądania i wykonaj przez ten sam `GrpcDynamicInvoker`, który wystawia backend
- **Dokumentacja proto** kształty żądań i odpowiedzi renderowane z deskryptorów i komentarzy serwera, więc czytasz kontrakt, który realnie wołasz.

### Jak jest budowane

`vite build` bundluje aplikację, `vite-plugin-singlefile` zwija ją do jednego samowystarczalnego pliku HTML, a `scripts/finalize-ui.mjs` przygotowuje go do osadzenia. Bezpieczeństwo typów jest egzekwowane z góry przez `svelte-check`. Żadnego osobnego serwera www, żadnych assetów z CDN, żadnego dryfu wersji: jedno assembly, jedno UI, zawsze zgodne z backendem, z którym płynie.

## Jak to się podpina

`DevToolsMiddleware` podąża za tą samą regułą co każde middleware wtyczek: odpowiada tylko na należące do niego ścieżki, resztę przepuszcza dalej nietkniętą. Podpięcie dzieje się przez jawne haki potoku hosta, więc włączanie i wyłączanie DevTools nigdy nie zmienia zachowania bazowej ścieżki żądań zobacz [ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/).

## Opcje

Wszystkim powyżej steruje `DevToolsOptions` w zakresowej sekcji konfiguracji wtyczki:

| Opcja | Domyślnie | Opis |
|-------|------------|------|
| `PathBase` | `/devtools` | Bazowa ścieżka UI i Swaggera |
| `GrpcUiPathBase` | `/grpc-ui` | Bazowa ścieżka UI gRPC |
| `GrpcTarget` | — | Nadpisanie docelowego adresu gRPC |
| `Swagger.RoutePrefix` | `swagger` | Ścieżka Swagger UI pod `PathBase` |
| `Swagger.DocumentName` | `v1` | Nazwa serwowanego dokumentu OpenAPI |
| `Swagger.DocumentTitle` | `AuthKit API` | Tytuł Swagger UI |
| `Swagger.Enabled` | `null` (Swagger tylko w Development) | Przełącznik Swagger UI |

Zmień ścieżkę, kiedy koliduje z Twoimi trasami wskaż `GrpcTarget` gdzie indziej, kiedy UI ma wołać innego hosta niż ten, który je serwuje.
