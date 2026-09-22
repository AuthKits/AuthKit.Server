[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/016-marten-and-wolverine-infrastructure/) | [Następny](/pl/adr/018-security-scheme-contract-explicit-handling/)

# [ADR-017] Wyodrębnianie poświadczeń klucza API poprzez strategie lokalizacji i formatu treści

*2026-09* | Status: accepted

**Tag:** #adr_017

**Date:** 2026-09-10

**Scope:** Host.Security

## Kontekst

Wtyczki eksponują mechanizmy uwierzytelniania jako niezależne od transportu metadane `AuthKitSecuritySchemeDescriptor` (ADR-009). Dla schematów klucza API deskryptor deklaruje, gdzie znajduje się poświadczenie, poprzez `AuthKitApiKeyLocation` (nagłówek, zapytanie, ciasteczko, metadane gRPC lub treść żądania). Host musi przekształcić te metadane w pobranie rzeczywistego poświadczenia z przychodzącego żądania asp.net, zweryfikować je i ustanowić uwierzytelnioną tożsamość bez wiązania autorów schematów z mechaniką HTTP.

## Problem

Pojedynczy ekstraktor/middleware, który wewnętrznie obsługuje wszystkie pięć lokalizacji oraz buforowanie treści, parsowanie JSON, parsowanie formularzy i normalizację wartości, staje się orkiestratorem i obiektem „boskim”. Dodanie nowej lokalizacji lub nowego formatu treści wymaga modyfikacji istniejącego kodu, a niskopoziomowa mechanika transportu (buforowanie, JSON, dekodowanie formularzy) jest splątana z przepływem orkiestracji (wyodrębnienie -> weryfikacja -> tożsamość).

## Decyzja

Pobieranie poświadczeń klucza API jest podzielone na małe strategie o pojedynczej odpowiedzialności, zarejestrowane w kontenerze DI hosta w `Host.Security`:

- **Strategie lokalizacji** implementują `IApiKeyLocationExtractor`, po jednej na `AuthKitApiKeyLocation` (`Header`, `Query`, `Cookie`, `GrpcMetadata`, `Body`). Każda strategia umie czytać tylko własną lokalizację i normalizować wartość.
- **`IApiKeyLocationExtractorRegistry`** (oparty na DI) mapuje `AuthKitApiKeyLocation` -> strategia. Rejestr rzuca `NotSupportedException` dla lokalizacji bez zarejestrowanej strategii, więc niewspierane konfiguracje zawodzą szybko.
- **Parsery formatu treści** implementują `IApiKeyBodyParser` i są wybierane według typu zawartości żądania. Obecnie zarejestrowane są `JsonApiKeyBodyParser` i `FormUrlEncodedApiKeyBodyParser`; nowy format to nowa klasa plus rejestracja.
- **`ApiKeyCredentialExtractor`** to cienki middleware, który tylko orkiestruje: rozwiązuje schemat, wyodrębnia poprzez rejestr, weryfikuje poprzez `IApiKeyValidator`, a po sukcesie dołącza oświadczenia jednostki jako uwierzytelnioną tożsamość. Nie zawiera logiki parsowania ani buforowania.
- **`IApiKeyValidator`** pozostaje kontraktem skierowanym do wtyczek do weryfikacji wyodrębnionego klucza, utrzymując semantykę poświadczeń poza hostem.
- **`ApiKeyCredentialExtractorOptions`** centralizuje wartości domyślne (nazwy pól nagłówka/zapytania/ciasteczka, próg buforowania treści) i jest konsumowany poprzez `IOptions`.
- Wszystko to jest rejestrowane przez `AddApiKeyCredentialExtraction(...)`.

Wyodrębnianie z treści buforuje żądanie poprzez `EnableBuffering`, odczytuje ładunek i przywraca pozycję strumienia, dzięki czemu dalsze middleware i kontrolery nadal widzą treść w stanie nienaruszonym.

### Uzasadnienie projektowe

- **Otwarte/Zamknięte**: nowa lokalizacja lub format treści dodaje klasę i rejestrację DI bez edytowania istniejącej strategii, parsera ani middleware.
- **Pojedyncza odpowiedzialność**: middleware jest właścicielem orkiestracji; każda strategia/parser jest właścicielem dokładnie jednego zagadnienia mechanicznego.
- **Odwrócenie zależności**: middleware zależy od abstrakcji rejestru i walidatora, a nie od parsowania `HttpContext` czy JSON.
- **Orkiestracja fail-open**: gdy nie wytworzono poświadczenia ani jednostki, potok jest kontynuowany, aby dalsze uwierzytelnianie zdecydowało o wyniku wyodrębnianie nigdy nie przerywa niepowiązanego żądania.

## Odrzucone

- Pojedynczy ekstraktor z instrukcjami switch/if i wbudowaną logiką treści oraz JSON każda nowa lokalizacja lub format modyfikuje orkiestrator.
- Opieranie decyzji parsowania na schemacie wewnątrz middleware wiąże orkiestrację z mechaniką formatu.
- Wymaganie od każdej wtyczki implementowania własnego wyodrębniania duplikuje parsowanie `HttpContext`, buforowanie i normalizację pomiędzy rozwiązaniami.
- Cicha konwersja niewspieranych lokalizacji (np. traktowanie `GrpcMetadata` jako zwykłych nagłówków HTTP bez zarejestrowanej strategii) ukrywa błędną konfigurację.

## Konsekwencje

`Host.Security` zawiera teraz wiele małych typów zamiast jednego dużego dodanie lokalizacji lub formatu kosztuje pliki i rejestrację, ale nigdy edycję istniejącego zachowania. Rejestr sprawia, że rzeczywiście niewspierane lokalizacje zawodzą szybko podczas rozwiązywania. Parsowanie treści jest selektywne względem typu zawartości, więc niejednoznaczne ładunki nie dają poświadczenia (fail-open), a nie błąd walidacja pozostaje zagadnieniem wtyczki za `IApiKeyValidator`, a host dołącza oświadczenia dopiero po pomyślnej weryfikacji.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - deskryptory i podłączane uwierzytelnianie wnoszone przez wtyczki
- [ADR-010](/pl/adr/010-plugin-loading-from-directory/) - slot middleware wtyczek, w którym działa wyodrębnianie
- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - transport metadanych gRPC reprezentowany jako nagłówki HTTP pisane małymi literami

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/016-marten-and-wolverine-infrastructure/) | [Następny](/pl/adr/018-security-scheme-contract-explicit-handling/)
