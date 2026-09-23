[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/025-plugin-options-openapi-and-marten-integrations/) | [Następny](/pl/adr/027-devtools-ui-typescript/)

# [ADR-026] Konfigurowanie uwierzytelniania i autoryzacji wtyczek poprzez infrastrukturę bezpieczeństwa hosta

*2026-09* | Status: accepted

**Tag:** #adr_026

**Date:** 2026-09-12

**Scope:** AuthKit.Plugins.Abstractions + Host security configuration

## Kontekst

Wtyczki mogą musieć dodawać schematy uwierzytelniania i polityki autoryzacji. Te rejestracje muszą uczestniczyć w tej samej infrastrukturze bezpieczeństwa ASP.NET Core co host i muszą pozostać kompatybilne z istniejącym JWT, middleware, routingiem endpointów oraz metadanymi schematów bezpieczeństwa.

## Decyzja

`IAuthKitPlugin` eksponuje opcjonalne domyślne hooki:

- `ConfigureAuthentication(AuthenticationBuilder)`;
- `ConfigureAuthorization(AuthorizationOptions)`.

Host wywołuje hooki w stabilnym porządku ID wtyczek. Przekazuje prawdziwe, należące do hosta buildery/opcje po zarejestrowaniu istniejącego domyślnego Keycloak JWT i przed zbudowaniem aplikacji. Schematy wtyczek nie stają się automatycznie domyślnym schematem uwierzytelniania. Polityki wtyczek są dodawane do tych samych `AuthorizationOptions` używanych przez ASP.NET Core.

Wyjątki hooków są propagowane. Nazwy schematów i polityk są globalnie znaczące; wtyczki powinny używać nazw z przestrzenią nazw, takich jak `plugin.read`. Host nie zmienia cicho nazw, nie zastępuje ani nie mapuje konfiguracji bezpieczeństwa. Zachowanie nowego transportu uwierzytelniania oraz deskryptora bezpieczeństwa OpenAPI pozostaje rządzone istniejącym kontraktem schematu bezpieczeństwa.

## Uzasadnienie

Używanie faktycznych builderów ASP.NET Core zachowuje standardową selekcję schematów, dostawców polityk, wyzwania uwierzytelniania, metadane autoryzacji endpointów oraz zachowanie middleware. Domyślne implementacje interfejsu utrzymują zgodność źródłową istniejących wtyczek.

## Odrzucone

- Osobny dostawca usług uwierzytelniania wtyczki odłączyłby schematy od hosta.
- Automatyczne czynienie schematów wtyczek domyślnymi mogłoby zmienić istniejące zachowanie hosta.
- Równoległy rejestr polityk omijałby `IAuthorizationPolicyProvider`.
- Przepisywanie deskryptorów schematów bezpieczeństwa na rejestracje uwierzytelniania myliłoby metadane dokumentacji z uruchomieniową konfiguracją bezpieczeństwa.

## Konsekwencje

Schematy i polityki wtyczek mogą chronić endpointy wtyczek poprzez normalne API ASP.NET Core. Kolidujące globalne nazwy pozostają błędami konfiguracji rządzonymi ścieżką konfiguracji hosta/frameworka i należy ich unikać poprzez przestrzenie nazw wtyczek. Istniejące zachowanie uwierzytelniania i autoryzacji JWT pozostaje domyślne, dopóki wtyczka jawnie nie wniesie dodatkowej konfiguracji.

## Powiązane

- [ADR-018](/pl/adr/018-security-scheme-contract-explicit-handling/) - metadane schematu bezpieczeństwa i mapowanie OpenAPI
- [ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/) - endpointy wtyczek i potok middleware
- [Issue #12](https://github.com/AuthKits/AuthKit.Server/issues/12) - hooki uwierzytelniania i autoryzacji

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/025-plugin-options-openapi-and-marten-integrations/) | [Następny](/pl/adr/027-devtools-ui-typescript/)
