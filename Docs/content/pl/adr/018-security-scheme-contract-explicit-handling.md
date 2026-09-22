[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/017-api-key-credential-extraction-strategies/) | [Następny](/pl/adr/019-plugin-metadata-attribute/)

# [ADR-018] Rozszerzenie kontraktu schematu bezpieczeństwa o jawne wsparcie hosta i OpenAPI

*2026-09* | Status: accepted

**Tag:** #adr_018

**Date:** 2026-09-10

**Scope:** AuthKit.Plugins.Abstractions

## Kontekst

Wtyczki eksponują mechanizmy uwierzytelniania jako niezależne od transportu metadane `AuthKitSecuritySchemeDescriptor` (ADR-009). Pierwotny kontrakt wyliczenia `AuthKitSecuritySchemeType` obejmował wyłącznie `ApiKey`, `Http`, `OAuth2` oraz `OpenIdConnect`, przy czym `OpenIdConnect` był aliasem `OAuth2` dzielącym wartość liczbową `2`. `AuthKitApiKeyLocation` obejmował `Header`, `Query` oraz `Cookie`. Każdy inny mechanizm (wzajemny TLS, sesje, schematy definiowane przez wtyczki, HTTP Basic) lub transport (metadane gRPC, treść żądania) musiał być przybliżany ciągami znaków albo przeciążaniem istniejących wartości.

## Problem

Przybliżanie niszczy bezpieczeństwo typów i pozwala hostowi mylić odrębne mechanizmy: uwierzytelnianie sesyjne z kluczem API przenoszonym w ciasteczku, HTTP Basic z generycznym uwierzytelnianiem HTTP, metadane gRPC z nagłówkami HTTP oraz poświadczenie w treści z poświadczeniem w nagłówku lub parametrze zapytania. Ponadto dołączony serializator OpenAPI (Swashbuckle 9.x / Microsoft.OpenApi 1.6.x) nie potrafi reprezentować wzajemnego TLS, sesji, schematów niestandardowych, metadanych gRPC ani lokalizacji w treści w swoim wyjściu OpenAPI 3.0. Podstawienie generycznego schematu nie tylko ukryłoby błędną konfigurację, ale wygenerowałoby wprowadzającą w błąd dokumentację dla klientów i generowanych SDK.

## Decyzja

Oba wyliczenia rosną addytywnie, a każda wartość jest obsługiwana jawnie przez hosta i przez mapper OpenAPI:

- `AuthKitSecuritySchemeType` zyskuje `MutualTls = 3`, `Session = 4`, `Custom = 5`, `Basic = 6`. Istniejące wartości pozostają nietknięte (`ApiKey = 0`, `Http = 1`, `OAuth2 = 2`). Historyczny alias zostaje usunięty: `OpenIdConnect` staje się odrębną wartością `7`, przy czym odstępstwo udokumentowano w uwagach; przenumerowanie istniejących wartości jest zabronione, ponieważ wartości liczbowe są częścią kontraktu wtyczki (`ApiKey`..`Basic` zajmują już `0`..`6`).
- `AuthKitApiKeyLocation` zyskuje `GrpcMetadata = 3` oraz `Body = 4`; istniejące wartości pozostają nietknięte.
- Wartości liczbowe są zadeklarowane jako część kontraktu wtyczki w obu wyliczeniach: nigdy nie wolno ich ponownie używać ani przenumerowywać.
- Host wylicza swoje możliwości jako `PluginContractValidator.SupportedSchemeTypes` oraz `SupportedApiKeyLocations`. `PluginContractValidator.Validate` sprawdza każdy zadeklarowany typ schematu i lokalizację względem tych zbiorów: wartości wspierane przechodzą, wartości zdefiniowane, lecz niewspierane oraz nieznane (przyszłe) zgłaszają `InvalidPluginContractException`. Nieznane wartości są identyfikowane po ich tożsamości liczbowej i nigdy nie są rozwiązywane do `Custom` ani żadnej znanej wartości.
- `AuthKitOpenApiSecuritySchemeMapper` mapuje każdą wartość jawnie: semantycznie poprawne reprezentacje OpenAPI dla `ApiKey`, `Http`, `OAuth2`, `OpenIdConnect` oraz `Basic` (jako uwierzytelnianie HTTP ze schematem `basic`); `NotSupportedException` dla wartości bez poprawnej reprezentacji 3.0 (`MutualTls`, `Session`, `Custom`; `GrpcMetadata` i `Body` jako lokalizacje klucza API); `ArgumentOutOfRangeException` dla wartości nieznanych.
- Wsparcie uruchomieniowe i reprezentowalność w OpenAPI są ortogonalne: host wspiera ekstrakcję poświadczeń `GrpcMetadata` i `Body` w czasie działania (ADR-017), mimo że OpenAPI 3.0 nie potrafi ich opisać. `RestfulConfiguration` przechwytuje błędy mappera, loguje ostrzeżenie i pomija definicję nigdy nie emituje generycznego zamiennika i nigdy nie przerywa generowania dokumentu.
- Do `Custom` powinna być dołączona opcjonalna, dostarczona przez wtyczkę `Description`; brak opisu generuje ostrzeżenie walidacji (nigdy mapowanie na schemat wbudowany).
- Host akceptuje wyłącznie `OpenApi:SpecVersion` `3.0`. `3.1`, która byłaby potrzebna do prawdziwej reprezentacji `mutualTLS`, jest odrzucana z `InvalidOperationException` zamiast cichego emitowania dokumentu 3.0.

### Uzasadnienie projektowe

- **Addytywny kontrakt**: stare wtyczki (w tym `DevTokens`) kompilują się i ładują bez zmian, a utrwalone deskryptory zachowują znaczenie.
- **Jawność zamiast sprytu**: każda wartość kontraktu ma nazwany przypadek w dokładnie dwóch miejscach (sprawdzenie możliwości hosta oraz mapper OpenAPI), zmuszając autorów do decyzji o wsparciu lub odrzuceniu każdej nowej wartości.
- **Szybkie zgłaszanie błędów**: niewspierany schemat kończy walidację wtyczki przy starcie, a niereprezentowalny jest pomijany w Swaggerze z zalogowanym powodem.
- **Żadnego fałszywego OpenAPI**: emitowanie `apiKey` dla wzajemnego TLS albo `Header` dla metadanych gRPC wygenerowałoby prawdopodobnie wyglądające, lecz błędne kontrakty klienckie.

## Odrzucone

- Przenumerowanie istniejących wartości wyliczeń dla czystszej sekwencji łamie każdą wydaną wtyczkę i utrwalony deskryptor; zabronione regułą niezmienności.
- Zachowanie aliasu `OpenIdConnect` = `OAuth2` dwa odrębne mechanizmy nie mogą dzielić tożsamości liczbowej; wtyczka deklarująca przepływ OIDC nie może deserializować się jako OAuth2.
- Generyczny mechanizm zapasowy w mapperze (nieznane -> `Custom`, `MutualTls` -> HTTP, `GrpcMetadata` -> `Header`, `Body` -> `Header`/`Query`/`Cookie`) ukrywa błędną konfigurację i emituje wprowadzającą w błąd dokumentację.
- Automatyczne podnoszenie generowanych dokumentów do OpenAPI 3.1 dla uzyskania `mutualTLS` dołączony stos serializatorów nie wspiera 3.1 host odrzuca ustawienie zamiast cichego obniżania wersji.
- Traktowanie `Session` jako klucza API w ciasteczku oraz `Basic` jako generycznego uwierzytelniania HTTP odtwarza niejednoznaczność, którą ten ADR usuwa.

## Konsekwencje

Dodawanie nowego mechanizmu uwierzytelniania lub transportu jest teraz addytywne: zdefiniuj wartość, rozszerz zbiór możliwości hosta, dodaj przypadek mappera i pokryj go testami pozytywnymi oraz testami braku mechanizmu zapasowego. Wartość OpenIdConnect znajduje się poza naturalną sekwencją `0`..`6` (udokumentowaną w uwagach wyliczenia). Wtyczki deklarujące niewspierane mechanizmy nie przechodzą walidacji kontraktu przy starcie; rozszerzenia hosta używają `PluginContractValidator.CreateCustom`. Wyjście Swaggera dla wtyczki mieszającej schematy reprezentowalne i niereprezentowalne zawiera wyłącznie definicje reprezentowalne, z zalogowanym ostrzeżeniem na pominięty schemat. Istnieją teraz dwie jawne gwarancje kontraktu: brak cichego mechanizmu zapasowego między schematami lub lokalizacjami oraz zawsze odrzucane nieznane przyszłe wartości.

## Powiązane

- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - deskryptory schematów bezpieczeństwa wtyczek
- [ADR-017](/pl/adr/017-api-key-credential-extraction-strategies/) - ekstrakcja uruchomieniowa dla nagłówka/zapytania/ciasteczka/metadanych gRPC/treści
- [ADR-013](/pl/adr/013-dual-rest-and-grpc-transport/) - powierzchnia transportu gRPC

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/017-api-key-credential-extraction-strategies/) | [Następny](/pl/adr/019-plugin-metadata-attribute/)
