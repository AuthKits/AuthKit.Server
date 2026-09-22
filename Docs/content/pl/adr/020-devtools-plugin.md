[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/019-plugin-metadata-attribute/) | [Następny](/pl/adr/021-swagger-serving-via-reflection/)

# [ADR-020] Hostowanie narzędzi deweloperskich poprzez dedykowaną wtyczkę DevTools

*2026-09* | Status: accepted

**Tag:** #adr_020

**Date:** 2026-09-11

**Scope:** Host + Plugins.Solutions.DevTools

## Kontekst

AuthKit potrzebuje narzędzi dla deweloperów do ćwiczenia własnych powierzchni: interaktywnej strony do wywoływania dowolnej eksponowanej metody gRPC oraz Swagger UI do przeglądania powierzchni REST/OpenAPI. Wcześniejsze prace eksponowały UI gRPC jako samodzielną wtyczkę `GrpcUI` i serwowały Swagger UI na samym hoście poprzez `UseSwagger` / `UseSwaggerUI` (bezpośredni middleware Swashbuckle). Oba narzędzia żyły w różnych domach architektonicznych: jedno było wtyczką, drugie było wbudowane na sztywno w potok żądań hosta.

## Problem

Narzędzie wbudowane w hosta może być włączone wyłącznie razem z hostem, wiąże hosta z middleware serwującym Swashbuckle i nie może być współdzielone między wariantami hosta ani usuwane bez edycji kodu hosta. Dedykowana wtyczka `GrpcUI` pokrywała gRPC, lecz pozostawiała narzędzia REST/OpenAPI poza granicą wtyczki dwa mechanizmy do tej samej pracy, dwa miejsca konfiguracji. Adresy URL narzędzi były też stałymi, niejawnymi ścieżkami hosta zamiast konfigurowalnych prefiksów należących do wtyczki.

## Decyzja

Oba narzędzia deweloperskie przenoszą się do jednej wtyczki `DevTools` (`src/Plugins/Solutions/DevTools`), zastępując samodzielną wtyczkę `GrpcUI` i usuwając serwowanie Swaggera z hosta:

- Wtyczka posiada własną przestrzeń URL poprzez `DevToolsOptions`: UI gRPC pod `GrpcUiPathBase` (`/grpc-ui`), Swagger UI w sekcji `Swagger` (domyślny prefiks trasy `swagger`, nazwa dokumentu `v1`, tytuł `AuthKit API`) oraz strona startowa pod `PathBase` (`/devtools`).
- UI gRPC działa w procesie: `GrpcServiceCatalog` skanuje domyślny kontekst ładowania złożeń pod kątem statycznych właściwości `ServiceDescriptor`, `GrpcDynamicInvoker` wykonuje metody jednoodpowiedziowe dynamicznie przez `Grpc.Net.Client` bez generowanych stubów, a `DevToolsMiddleware` obsługuje `/grpc-ui` oraz jego endpointy `/api/services` + `/api/invoke`.
- Generowanie dokumentu Swagger pozostaje w hoście (`RestfulConfiguration` + `AddSwaggerGen`); wyłącznie middleware SERWUJĄCY Swagger przenosi się do `DevTools` poprzez `SwaggerHost`.
- Host nie wywołuje już `UseSwagger` / `UseSwaggerUI`; `AppMiddlewareConfiguration` zawiera wyłącznie slot wtyczki. Narzędzia deweloperskie są obecne we wdrożeniu dokładnie wtedy, gdy wtyczka jest wdrożona.
- Serwowanie Swagger UI jest bramkowane: domyślnie włączone tylko w środowisku deweloperskim (`Swagger.Enabled ?? environment.IsDevelopment()`), z przypięciem wersji specyfikacji OpenAPI serwowanego dokumentu.
- Host jest publikowany z domyślnie wybranym `DevTools` (plik rozwiązania i Dockerfile), więc domyślne wdrożenie zachowuje dostępność obu narzędzi.

### Uzasadnienie projektowe

- **Jedna powierzchnia narzędziowa**: REST (Swagger) i gRPC (UI w procesie) są obiema interaktywnymi reprezentacjami tego samego hosta jedna wtyczka posiada doświadczenie deweloperskie.
- **Spójna granica wtyczki**: wszystko, co odwiedza deweloper szablonu, jest wnoszone przez wtyczkę, pod prefiksami ścieżek należącymi do wtyczki, usuwalne poprzez niewdrażanie wtyczki.
- **W procesie zamiast proxy**: UI gRPC rozmawia z hostem bezpośrednio zamiast stawiać czoła osobnemu procesowi i używać refleksji gRPC, utrzymując odkrywanie lokalnie i konfigurację na minimalnym poziomie.
- **Bez potrzeby stubów**: dynamiczne wywoływanie na deskryptorach metod oznacza, że wtyczka nigdy nie potrzebuje generowanego kodu klienta dla renderowanych usług.

## Odrzucone

- Zachowanie serwowania Swaggera w hoście (`UseSwagger`/`UseSwaggerUI`) wiąże hosta z konkretnym narzędziem i konkretnym middleware serwującym.
- Samodzielna wtyczka `GrpcUI` z należącym do hosta Swaggerem dwa domy dla równoważnej funkcjonalności.
- Proces proxy gRPC (np. utrzymywany zewnętrzny binarny UI gRPC) dla wsparcia strumieni i kondycji dodatkowy proces wdrożeniowy i przeskok transportowy; dodatkowe wsparcie uznano za zbędne dla wewnętrznego narzędzia deweloperskiego.
- Serwowanie Swaggera z wtyczki przy jednoczesnym przeniesieniu tam generowania dokumentu powieliłoby konfigurację `AddSwaggerGen` generowanie pozostaje w hoście, serwowanie pozostaje we wtyczce.

## Kontrakt HTTP API

Wtyczka DevTools eksponuje dwa endpointy JSON API konsumowane przez frontend TypeScript:

### `/api/services` GrpcCatalogResponse

Zwraca odkryte usługi gRPC dla katalogu:

```csharp
public sealed class GrpcCatalogResponse
{
    public required string Target { get; init; }
    public required IEnumerable<GrpcServiceInfo> Services { get; init; }
}

public sealed class GrpcServiceInfo
{
    public required string Name { get; init; }      // Short service name
    public required string FullName { get; init; }  // Fully qualified name
    public string? Description { get; init; }       // Nullable proto doc comment (null = absent)
    public required string Package { get; init; }
    public required string FileName { get; init; }
    public bool IsPlugin { get; init; }              // Default: false (backward-compatible for host services)
    public required IEnumerable<GrpcMethodInfo> Methods { get; init; }
}
```

- `Description`: wartość nullable typu string z dokumentacji proto. Nieobecna (`null`), gdy proto nie ma komentarza.
- `IsPlugin`: domyślnie `false` dla usług hosta, co zapewnia zgodność wsteczną ze złożeniami niebędącymi wtyczkami.

### `/api/invoke` GrpcInvocationRequest → GrpcInvocationResult

Wykonuje jednoodpowiedziową metodę gRPC:

```csharp
public sealed class GrpcInvocationRequest
{
    public required string Service { get; init; }
    public required string Method { get; init; }
    public required string RequestJson { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}

public sealed class GrpcInvocationResult
{
    public required bool Success { get; init; }
    public required string StatusName { get; init; }
    public required int StatusCode { get; init; }
    public string? Detail { get; init; }
    public string? ResponseJson { get; init; }
    public string? ResponseBase64 { get; init; }    // Success-only base64-encoded protobuf payload; null on failure
    public double ElapsedMs { get; init; }
    public IReadOnlyDictionary<string, string>? Trailers { get; init; }
}
```

- `ResponseBase64`: wypełniane wyłącznie przy udanych wywołaniach (`Success == true`). Zawiera surowe bajty protobuf zakodowane jako base64.
- **Limit rozmiaru ładunku**: ładunek base64 jest ograniczony do `int.MaxValue - 1` bajtów (≈2 GB) z powodu ograniczeń rozmiaru protobuf.
- `ResponseJson`: obecne wyłącznie przy powodzeniu, zawierające odpowiedź sformatowaną jako JSON poprzez formater JSON protobuf.
- Nieudane wywołania zwracają `Success = false` z wypełnionymi `Detail` i `Trailers` do debugowania.

### Serializacja JSON

Wszystkie DTO są serializowane przy użyciu `System.Text.Json` z `JsonSerializerDefaults.Web` dla spójnej konwencji wielkości liter:

- Nazwy właściwości w JSON w konwencji PascalCase (np. `"ResponseBase64"`, `"IsPlugin"`).
- Pola nullable są pomijane, gdy są null (domyślne zachowanie JSON ASP.NET Core).
- Pola boolowskie o wartości `false` są uwzględniane (nie pomijane).

## Konsekwencje

`AppMiddlewareConfiguration` hosta jest mniejsze i nie odwołuje się już do Swashbuckle. Rozwiązanie `DevTools` posiada kod narzędziowy przyjazny kompilatorowi (katalog, invoker, middleware, opcje i zasoby UI). Dostępność Swagger UI zależy od wdrożenia wtyczki i bramkowania środowiskowego zamiast konfiguracji budowania hosta. UI gRPC wspiera wyłącznie metody jednoodpowiedziowe metody strumieniowe są raportowane jako niewspierane zamiast przybliżane. Konfiguracja jest scentralizowana w `DevToolsOptions`, rozwiązywana ze zmiennymi środowiskowymi (`GRPC_UI_TARGET`, `DEV_CERT_PORT_GRPC`) do lokalnego dewelopmentu przeciw punktowi końcowemu HTTPS gRPC.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - kontrakt wtyczki i dynamiczne odkrywanie
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - slot middleware wtyczki używany przez DevToolsMiddleware
- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - ćwiczone powierzchnie dualne REST + gRPC
- [ADR-021](/pl/adr/021-swagger-serving-via-reflection/) - jak SwaggerHost serwuje SwaggerUI poprzez refleksję

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/019-plugin-metadata-attribute/) | [Następny](/pl/adr/021-swagger-serving-via-reflection/)
