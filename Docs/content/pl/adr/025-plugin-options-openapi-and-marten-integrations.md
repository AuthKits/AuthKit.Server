[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/024-plugin-lifecycle-and-hosted-services/) | [Następny](/pl/adr/026-plugin-authentication-and-authorization-hooks/)

# [ADR-025] Utrzymywanie jawnych integracji opcji, OpenAPI i Marten wtyczek

*2026-09* | Status: accepted

**Tag:** #adr_025

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host + optional plugin integrations

## Kontekst

Wtyczki potrzebują silnie typowanych opcji, wkładu do OpenAPI oraz opcjonalnej konfiguracji dokumentów Marten. Te integracje mają różne granice zależności: opcje należą do bazowego kontraktu, podczas gdy Swashbuckle i Marten są zagadnieniami infrastrukturalnymi.

## Decyzja

`IAuthKitPlugin.BindConfiguration<TOptions>` wiąże poprzez standardowy system opcji .NET z `Plugins:{Name}`. Wtyczki otrzymują więc normalne usługi `IOptions<T>`, `IOptionsSnapshot<T>` lub `IOptionsMonitor<T>` bez własnego rejestru.

Kontrakty integracji OpenAPI i Marten żyją w opcjonalnym projekcie `AuthKit.Plugins.Integrations`:

- `IOpenApiPlugin.ConfigureOpenApi(SwaggerGenOptions)` otrzymuje faktyczne opcje Swagger należące do hosta;
- `IMartenPlugin.ConfigureMarten(StoreOptions)` otrzymuje faktyczne opcje Marten należące do hosta.

Host wywołuje oba hooki w stabilnym porządku ID wtyczek. Hooki OpenAPI działają wewnątrz istniejącej konfiguracji `AddSwaggerGen`. Hooki Marten działają wewnątrz istniejącej konfiguracji `AddMarten`. Wyjątki mogą przerywać konfigurację hosta; wkłady nigdy nie są cicho odrzucane.

## Uzasadnienie

Bazowy kontrakt wtyczki pozostaje niezależny od opcjonalnej infrastruktury trwałości i dokumentacji. Wtyczki potrzebujące którejś integracji referencjonują opcjonalny projekt integracji, podczas gdy zwykłe wtyczki zachowują mniejszą zależność abstrakcji.

## Odrzucone

- Dodanie referencji Swashbuckle lub Marten do bazowego projektu abstrakcji zmusiłoby niepowiązane wtyczki do zależenia od opcjonalnej infrastruktury hosta.
- Własny rejestr opcji powieliłby standardowe mechanizmy opcji i DI .NET.
- Tworzenie osobnych instancji opcji Swagger lub Marten odłączyłoby wkłady wtyczek od faktycznej konfiguracji hosta.

## Konsekwencje

Konfiguracja wtyczek jest izolowana i silnie typowana. Wkłady OpenAPI i Marten są deterministyczne i dzielą obiekty konfiguracji należące do hosta. Hosty bez Marten nie muszą referencjonować kontraktu integracji ani wywoływać jego hooka.

## Powiązane

- [ADR-022](/pl/adr/022-plugin-configuration-context-and-builder/) - kontekst konfiguracji wtyczki
- [ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/) - hooki integracji aplikacji
- [Issue #11](https://github.com/AuthKits/AuthKit.Server/issues/11) - wymagania integracji opcji, OpenAPI i Marten

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/024-plugin-lifecycle-and-hosted-services/) | [Następny](/pl/adr/026-plugin-authentication-and-authorization-hooks/)
