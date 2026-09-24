[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/029-structured-plugin-health-contract/) | [Następny]()

# [ADR-030] Deklaratywny Pipeline Middleware Pluginów Z Jawnym Transportem

*2026-09* | Status: accepted

**Tag:** #adr_030

**Date:** 2026-09-24

**Scope:** AuthKit.Plugins.Abstractions + Host + PluginContractValidator

## Context

ADR-023 dał pluginom jawne hooki pipeline (`ConfigureApplication`, `ConfigurePipeline`, `MapEndpoints`) oraz legacy slot `MiddlewareType`. PR #51 rozszerza to o deklaratywne wpisy middleware: plugin rejestruje wiele rekordów `PluginMiddleware` z jawnym `Transport` (`Http`/`Grpc`), semantyczną `PipelinePosition`, kluczem `Order` i flagą włączenia, a Host odpowiada za kompozycję i walidację. Ten ADR opisuje, dlaczego pipeline jest deklaratywny i transport-explicit, zamiast opierać się na refleksji czy mostkowaniu.

## Problem

Pojedynczy implicit slot nie wyraża kolejności między pluginami, włączania per-wpis ani różnic między middleware HTTP a interceptorami gRPC. Zgadywanie transportu przez refleksję ukrywa błędy konfiguracji, a mostkowanie middleware `HttpContext` do wywołań gRPC psuje ramkowanie gRPC. Stary slot `MiddlewareType` został więc usunięty zamiast trzymania go jako ścieżki kompatybilności.

## Decision

- Plugin deklaruje *co* przez `PluginMiddleware` (deklaracja typów, bez fabryk): `MiddlewareType`, semantyczna `Position` (`BeforeRouting` … `AfterEndpointExecution`), `Order`, `IsMiddlewareEnabled`, `Name`, jawny `Transport`.
- Host jest właścicielem *aktywacji i kompozycji*: wpisy HTTP składane per pozycja w deterministycznej kolejności (`Order → PluginId → indeks deklaracji`) `IAuthKitMiddleware` / `AuthKitMiddlewareBase` resolvowane per request z request service provider typy konwencyjne przez `UseMiddleware`. Wpisy gRPC muszą być podklasami `Interceptor` wpiętymi w natywny łańcuch — bez drugiego frameworka interceptorów i bez mostka `HttpContext`.
- Middleware HTTP nigdy nie działa na wywołaniach gRPC (pomijane przez branch na content-type) wpisy HTTP only są logowane jako pominięte z podpowiedzią deklaracji `Transport = Grpc`.
- `PluginContractValidator` (`MiddlewareRule`) odrzuca fail fast naruszenia strukturalne: niejednoznaczne modele AuthKit, statyczne/void `Invoke`, wielokonstruktorowe typy konwencyjne, typy generyczne/abstrakcyjne, mismatch `Transport`/`MiddlewareType`, niezdefiniowane wartości `Transport`/`Position`. Wyłączone wpisy też są walidowane, żeby nie dało się ukryć kontraktu przez wyłączenie.
- Nie ma legacy pojedynczego slotu: stara właściwość `MiddlewareType` i jej ścieżka kompatybilności w hoście zostały usunięte. Cały middleware deklarowany jest przez `Middlewares`.

### Design Rationale

- Jawny `Transport` eliminuje zgadywanie przez refleksję: błędna deklaracja kończy się diagnostyką z nazwą pluginu i typu zamiast cichego działania na złym transporcie.
- Pozycje semantyczne są neutralne transportowo w intencji (`AfterAuthorization` znaczy po uwierzytelnieniu *i* autoryzacji), a Host mapuje je per transport.
- Deterministyczna kolejność (`Order → PluginId → indeks`) uniezależnia kompozycję od kolejności discovery i nadaje się do testów.
- Resolwowanie per-request w jednym scope gwarantuje spójne scoped services w middleware pluginów.

## Rejected

- Wnioskowanie transportu przez refleksję: akceptuje błędne deklaracje i daje runtime failures z dala od miejsca deklaracji.
- Mostkowanie `HttpContext` do gRPC: psuje ramkowanie dla middleware zapisującego lub short circuitującego odpowiedź.
- Drugi framework interceptorów obok `Grpc.Core.Interceptors.Interceptor`: duplikuje natywny łańcuch bez zysku behawioralnego.
- Ciche odrzucanie niezdefiniowanych `Transport`/`Position` i niezatwierdzonych wyłączonych wpisów: ukrywa błędy konfiguracji (review CodeRabbit na PR #51 wyłapał dokładnie to).
- Trzymanie starego pojedynczego slotu `MiddlewareType` obok `Middlewares`: jeden implicit slot nie wyraża kolejności, włączania per wpis ani transportu, a dwie ścieżki grożą duplikacją rejestracji i rozjazdem walidacji. DevTokens i DevTools zmigrowano do deklaratywnych wpisów `BeforeAuthentication`.

## Consequences

- Autorzy pluginów muszą jawnie deklarować transport i wybierać wspieraną pozycję semantyczną błędne deklaracje padają na walidacji/starcie zamiast psuć runtime.
- Middleware HTTP, które wcześniej (błędnie) działało też na wywołaniach gRPC, już tego nie robi zachowanie gRPC wymaga `Interceptor`.
- Nie ma wstecznie kompatybilnego slotu middleware: istniejące pluginy muszą zmigrować `MiddlewareType` do wpisu `Middlewares` (DevTokens/DevTools używają `BeforeAuthentication`) nie zadeklarowany middleware już się nie odpala.
- Reguły walidatora i hosta muszą ewoluować razem: każdy nowy model middleware wymaga gałęzi walidacji i ścieżki aktywacji w hoście.

## Powiązane

- [ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/) - jawne hooki pipeline hosta i legacy slot middleware
- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - dualny transport REST i gRPC
- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - discovery pluginów i granica kontraktu

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/029-structured-plugin-health-contract/) | [Następny]()
