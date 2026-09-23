[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/020-devtools-plugin/) | [Następny]()

# [ADR-021] Serwowanie Swaggera poprzez opartego na refleksji SwaggerHost w DevTools

*2026-09* | Status: accepted

**Tag:** #adr_021

**Date:** 2026-09-11

**Scope:** Plugins.Solutions.DevTools

## Kontekst

ADR-020 przeniósł serwowanie Swagger UI z hosta do wtyczki `DevTools`. Wtyczka konsumowała już dostawcę dokumentu OpenAPI (`ISwaggerProvider`, rejestrowanego przez `RestfulConfiguration` + `AddSwaggerGen`), lecz faktyczna serializacja dokumentu i renderowanie HTML UI były wykonywane przez `SwaggerMiddleware` i `SwaggerUIMiddleware` Swashbuckle z których oba są **wewnętrzne** dla swoich pakietów NuGet, więc wtyczka nie może ich wywoływać tak, jak robił to wcześniej host.

## Problem

Wtyczka musi odtworzyć to, co host robił wcześniej publicznymi metodami rozszerzającymi `UseSwagger` / `UseSwaggerUI`, lecz stojące za tymi rozszerzeniami middleware nie są częścią publicznej powierzchni API. Wtyczka musi też kontrolować wersję specyfikacji OpenAPI przypiętą dla serwowanego dokumentu niezależnie od dokumentu, który `RestfulConfiguration` mógłby generować dla innych konsumentów.

## Decyzja

`DevTools` serwuje Swaggera przy użyciu opartego na refleksji `SwaggerHost`:

- `SwaggerHost` rozwiązuje wewnętrzne typy `Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware` oraz `Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware` po nazwie kwalifikowanej złożeniem przy starcie, konstruuje jedną instancję na middleware poprzez `Activator.CreateInstance` ze standardowym kształtem konstruktora (notFound, options) i wywołuje wewnętrzne metody `Invoke` refleksyjnie na żądanie.
- Middleware dokumentu Swagger jest konfigurowane szablonem trasy pod skonfigurowanym prefiksem trasy Swagger oraz rozwiązaną wersją specyfikacji OpenAPI.
- Middleware Swagger UI otrzymuje opcje UI (prefiks trasy, tytuł dokumentu, `IndexStream` wskazujący osadzony zasób `index.html`) oraz punkt końcowy wskazujący trasę dokumentu.
- `DevToolsMiddleware` decyduje, czy ścieżka żądania należy do wtyczki (UI gRPC, Swagger, strona startowa) i dla segmentu Swagger deleguje do `SwaggerHost.TryServeAsync`; 404-not-found `RequestDelegate` jest przekazywany do middleware Swashbuckle, gdy ścieżka nie pasuje.
- Wersja specyfikacji OpenAPI serwowanego dokumentu jest rozwiązywana przez `SwaggerHost.ResolveSpecVersion`: nadpisanie `DevTools:Swagger:SpecVersion`, potem hostowe `OpenApi:SpecVersion`, domyślnie `3.0`. Wspierane wartości to `3.0.x` oraz `3.1.x`; nieznana wartość loguje ostrzeżenie i wraca do `3.0`.
- Ponieważ serwowany dokument używa wersji specyfikacji przypiętej dla DevTools, schematy wzajemnego TLS (ADR-018) pojawiają się natywnie (`mutualTLS`) wyłącznie wtedy, gdy dokument jest przypięty do OpenAPI 3.1.

### Uzasadnienie projektowe

- **Ponowne użycie zamiast reimplementacji**: wtyczka nie reimplementuje serializacji OpenAPI ani HTML UI steruje dokładnie tym middleware, którego używał host, poprzez refleksję, ponieważ typy są wewnętrzne.
- **Jednokrotna konstrukcja**: instancje middleware są budowane raz przy starcie wtyczki i używane ponownie dla każdego żądania; pracą na żądanie jest refleksyjne wywołanie, nie przebudowa.
- **Niezależne przypięcie**: DevTools kontroluje własną wersję specyfikacji dokumentu zamiast dziedziczyć stały dokument hosta, co pozwala działać w DevTools przypadkom mappera OpenAPI zależnym od 3.1 (ADR-018).

## Odrzucone

- Reimplementacja strony Swagger UI oraz serializacji OpenAPI wewnątrz wtyczki powiela dużą, utrzymywaną powierzchnię bez zysku architektonicznego.
- Zależność od wewnętrznych typów Swashbuckle jako twarde odwołanie biblioteczne są wewnętrzne z założenia; twarde odwołanie po prostu by się nie skompilowało.
- Kopiowanie/wysyłanie źródeł middleware Swashbuckle do wtyczki obciążenie licencyjne i utrzymaniowe oraz dryf względem poprawek upstream.
- Całkowite usunięcie serwowania Swaggera z DevTools (tylko dokument JSON pod znanym URL) traci interaktywne UI, które motywowało wtyczkę.

## Konsekwencje

`DevTools` pozostaje na wersji Swashbuckle dostarczającej wewnętrzne middleware; aktualizacja wersji Swashbuckle mogłaby zmienić sygnatury wewnętrznych typów/metod i złamać `SwaggerHost` przy starcie (fail-fast z opisowym wyjątkiem). Warstwa refleksji to celowy, widoczny koszt: dwa wyszukiwania `Type`/`MethodInfo` oraz `Invoke` na żądanie zamiast bezpośrednich wywołań. Nieznane wersje specyfikacji degradują się do ostrzeżenia + 3.0 w serwowaniu DevTools (więc UI nadal działa), co jest celowo bardziej pobłażliwe niż `RestfulConfiguration`, które twardo zgłasza błąd przy nieznanych wersjach specyfikacji w konfiguracji aplikacji.

## Powiązane

- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - powierzchnia OpenAPI serwowana przez DevTools
- [ADR-020](/pl/adr/020-devtools-plugin/) - dlaczego serwowanie Swaggera w ogóle żyje we wtyczce
- [ADR-018](/pl/adr/018-security-scheme-contract-explicit-handling/) - schematy wzajemnego TLS wymagają przypięcia 3.1, które dostarcza DevTools

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/020-devtools-plugin/) | [Następny]()
