[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/012-token-key-bindings-persisted-in-marten/) | [Następny](/pl/adr/014-error-responses-via-middleware/)

# [ADR-013] Udostępnianie zarówno powierzchni transportowej REST, jak i gRPC

*2026-08* | Status: accepted

**Tag:** #adr_013

**Date:** 2026-08-26

**Scope:** Host

## Kontekst

Host musi obsługiwać klientów API o różnych potrzebach konwencjonalnych konsumentów HTTP/JSON oraz wydajnych klientów gRPC działających w podejściu contract-first. Obie powierzchnie opierają się na tej samej domenie Core i wkładzie wtyczek.

## Problem

Zobowiązanie do jednego transportu albo wyklucza klasę klientów, albo wymusza niezręczną warstwę adaptacyjną. Uruchamianie dwóch oddzielnych procesów duplikuje kompozycję, konfigurację i powierzchnię operacyjną.

## Decyzja

Host jest pojedynczą aplikacją ASP.NET Core, która udostępnia zarówno powierzchnię REST (kontrolery/MVC poprzez `AddRestfulServices` i `MapAppEndpoints`), jak i powierzchnię gRPC (`AddGrpcServices` i `MapGrpcEndpoints`) z jednego złożonego potoku. Obie opierają się na tych samych usługach Core, tym samym wkładzie wtyczek (ADR-009/010) i tej samej infrastrukturze (ADR-016).

### Uzasadnienie projektowe

- Jeden proces i jeden korzeń kompozycji utrzymują wspólną konfigurację, DI, middleware i ładowanie wtyczek dla obu transportów.
- REST zapewnia szeroką interoperacyjność HTTP/JSON; gRPC zapewnia typowane, niskonarzutowe kontrakty z tej samej domeny.
- Jeden potok zapobiega rozjazdom między tym, co potrafi każdy transport.

## Odrzucone

- Udostępnianie wyłącznie REST albo wyłącznie gRPC.
- Dzielenie powierzchni na oddzielne wdrażalne procesy.
- Tunelowanie jednego transportu przez drugi zamiast natywnych punktów końcowych.

## Konsekwencje

Klienci mogą wybrać transport, który im odpowiada, bez drugiego wdrożenia, a oba pozostają spójne z Core. Kosztem jest utrzymywanie dwóch zestawów kontraktów (OpenAPI i protobuf) oraz dbanie o to, aby obie powierzchnie odzwierciedlały to samo zachowanie domenowe i dostarczane przez wtyczki schematy bezpieczeństwa.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - obie powierzchnie eksponują wkład wtyczek
- [ADR-014](/pl/adr/014-error-responses-via-middleware/) - ujednolicone renderowanie błędów dla obu transportów
- [ADR-015](/pl/adr/015-keycloak-external-jwt-authority/) - uwierzytelnianie chroniące obie powierzchnie

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/012-token-key-bindings-persisted-in-marten/) | [Następny](/pl/adr/014-error-responses-via-middleware/)
