[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/013-dual-rest-and-grpc-transport/) | [Następny](/pl/adr/015-keycloak-external-jwt-authority/)

# [ADR-014] Renderowanie błędów HTTP jako Problem Details wg RFC 7807 poprzez middleware

*2026-08* | Status: accepted

**Tag:** #adr_014

**Date:** 2026-08-26

**Scope:** Host.Restful.Middleware.Exceptions

## Kontekst

ADR-008 stanowi, że Core jest właścicielem współdzielonego słownika błędów (`DomainException`, `ErrorResponse`, `ErrorMetadataOptions`). Host musi przekształcać niepowodzenia naruszenia domenowe, błędy walidacji i wyniki autoryzacji w spójny kształt HTTP bez ujawniania wewnętrznych szczegółów.

## Problem

Rozpraszanie obsługi błędów w każdym kontrolerze daje niespójne odpowiedzi i grozi ujawnieniem śladów stosu. Format na styku musi być standardowy i maszynowo czytelny oraz musi pozostać powiązany ze słownikiem błędów Core.

## Decyzja

Trzy komponenty hosta centralizują renderowanie błędów HTTP, wszystkie emitując `ProblemDetails` wg RFC 7807 z polami `error_code`, `trace_id` oraz dokumentacyjnym `type` budowanym z `ErrorMetadataOptions.DocsBaseUrl`:

- `ExceptionHandlingMiddleware` mapuje `DomainException` na `409 Conflict` (kod wywodzony z typu wyjątku), a każdy inny wyjątek na `500` bez wewnętrznych szczegółów.
- `ValidationExceptionMiddleware` mapuje FluentValidation `ValidationException` na `400 Bad Request` z błędami pogrupowanymi według właściwości.
- `CustomAuthorizationMiddlewareResultHandler` mapuje challenge/forbidden na `401`/`403`.

### Uzasadnienie projektowe

- Potok middleware przechwytuje błędy w jednym miejscu, dzięki czemu kontrolery pozostają czyste, a odpowiedzi jednolite.
- RFC 7807 to standardowy format problemów rozumiany przez klientów HTTP i generatory kodu.
- Mapowanie z `DomainException` sprawia, że słownik błędów Core (ADR-008) pozostaje jedynym źródłem domenowego znaczenia błędów, podczas gdy host wybiera konkretną reprezentację na styku.

## Odrzucone

- Zwracanie kształtu DTO `ErrorResponse` z Core bezpośrednio jako jedynego kontraktu HTTP (mniej przyjazne narzędziom niż ProblemDetails).
- Pozwalanie wyjątkom propagować się do domyślnej strony frameworka.
- Ujawnianie komunikatów wyjątków lub śladów stosu dla błędów niedomenowych.

## Konsekwencje

Klienci HTTP otrzymują przewidywalne, oparte na standardach ciało błędu z kodami i identyfikatorami śledzenia, a błędy domenowe pozostają tworzone w Core. Kosztem jest utrzymywanie dwóch powiązanych, lecz odrębnych kształtów słownika `ErrorResponse` z Core oraz renderowania `ProblemDetails` hosta oraz synchronizowanie mapowania w miarę pojawiania się nowych wyjątków domenowych.

## Powiązane

- [ADR-008](/pl/adr/008-standardized-error-response/) - słownik błędów Core renderowany tutaj
- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - dotyczy powierzchni REST

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/013-dual-rest-and-grpc-transport/) | [Następny](/pl/adr/015-keycloak-external-jwt-authority/)
