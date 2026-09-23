[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/008-standardized-error-response/) | [Następny](/pl/adr/010-plugin-loading-from-directory/)

# [ADR-009] Odkrywanie i dynamiczne ładowanie wtyczek poprzez kontrakt IAuthKitPlugin

*2026-08* | Status: accepted

**Tag:** #adr_009

**Date:** 2026-08-26

**Scope:** AuthKit.Plugins.Abstractions

## Kontekst

AuthKit.Server składa się z hosta oraz opcjonalnych pakietów rozwiązań (na przykład rozwiązania tokenów deweloperskich `DevTokens`). Host musi móc rozszerzać swój zestaw funkcji nowe usługi, middleware, kontrole kondycji i schematy uwierzytelniania bez rekompilacji ani bezpośredniego odwoływania się do każdego rozwiązania w czasie budowania.

## Problem

Jeśli rozwiązania są podłączane do hosta poprzez statyczne referencje projektów i ręcznie pisaną rejestrację, host wiąże się z każdą funkcją, a każda nowa możliwość staje się zmianą hosta. Host potrzebuje też jednolitego sposobu, aby rozszerzenie mogło wnieść wkład do kontenera DI, potoku żądań, powierzchni kondycji oraz metadanych OpenAPI, pozostając jednocześnie odizolowanym za stabilnymi abstrakcjami.

## Decyzja

Wtyczki implementują kontrakt `IAuthKitPlugin` i są odkrywane oraz ładowane dynamicznie przez hosta podczas startu. Wtyczka **nie** musi być bezpośrednio referencjonowana przez projekt hosta. Rozszerzenie wnosi wkład do działającego serwera poprzez dobrze zdefiniowane, opcjonalne elementy kontraktu.

### IAuthKitPlugin

**Obowiązki:**

- Identyfikowanie wtyczki (`Name`, `Version`, opcjonalny `Description`) na potrzeby diagnostyki i metadanych.
- Rejestrowanie usług wtyczki w kontenerze DI hosta poprzez `ConfigureServices` (wtyczka nie może tworzyć własnego kontenera).
- Opcjonalne dostarczenie typu middleware ASP.NET Core (`MiddlewareType`) wstawianego przez hosta w slocie potoku wtyczek.
- Opcjonalne udostępnienie sygnału kondycji poprzez `CheckHealthAsync`, rozwiązywanego z głównego dostawcy usług hosta.
- Opcjonalne dostarczenie niezależnych od transportu schematów bezpieczeństwa OpenAPI poprzez `GetSecuritySchemes`.

### AuthKitSecuritySchemeDescriptor

**Obowiązki:**

- Opisywanie mechanizmu uwierzytelniania (klucz API, HTTP, OAuth2, OpenID Connect) na poziomie metadanych.
- Pozostawanie niezależnym od transportu, tak aby ten sam schemat mógł obsługiwać nagłówki HTTP i metadane gRPC.

### Uzasadnienie projektowe

- Dynamiczne odkrywanie utrzymuje hosta w separacji od konkretnych rozwiązań i pozwala dostarczać funkcje jako pakiety typu drop-in.
- Jeden kontrakt z sensownymi domyślnymi implementacjami elementów opcjonalnych sprawia, że wtyczki są małe, a kod integracyjny hosta jednolity.
- Izolacja za `AuthKit.Plugins.Abstractions` oznacza, że wtyczki zależą wyłącznie od kontraktu, a nie od wewnętrznych mechanizmów hosta klucze podpisujące i powiązania tokenów z kluczami pozostają własnością rdzenia i są konsumowane poprzez DI, a nie reimplementowane.

## Odrzucone

- Statyczne referencje projektów i rejestracja w czasie kompilacji dla każdego rozwiązania.
- Pozwalanie każdej wtyczce na tworzenie własnego kontenera wstrzykiwania zależności i zarządzanie nim.
- Kodowanie middleware wtyczek lub kontroli kondycji na sztywno w hoście zamiast odkrywania ich z kontraktu.
- Osadzanie zależnych od transportu szczegółów uwierzytelniania bezpośrednio w kodzie wtyczki zamiast poprzez niezależny od transportu deskryptor.

## Konsekwencje

Host pozostaje stabilny, podczas gdy funkcje są dodawane jako niezależnie ładowalne wtyczki, a każda wtyczka integruje się poprzez jedną przewidywalną powierzchnię. Kosztem jest mechanizm odkrywania/ładowania podczas startu oraz dyscyplina utrzymywania zachowania wtyczek w granicach kontraktu wtyczki muszą polegać na wstrzykiwanych usługach rdzenia (takich jak klucze podpisujące), zamiast sięgać do wewnętrznych mechanizmów hosta.

## Powiązane

- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - odkrywanie i ładowanie wtyczek po stronie hosta
- [ADR-004](/pl/adr/004-token-key-bindings-domain/) - wtyczki konsumują powiązania rdzenia poprzez kontrakt
- [ADR-016](/pl/adr/016-marten-and-wolverine-infrastructure/) - procedury obsługi wtyczek działają na tej infrastrukturze

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/008-standardized-error-response/) | [Następny](/pl/adr/010-plugin-loading-from-directory/)
