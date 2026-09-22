# Wprowadzenie

AuthKit to **wtyczkowa usługa** do **uwierzytelniania deweloperów, wydawania tokenów SDK oraz weryfikacji dostępu**.

Zapewnia, że tylko autoryzowani deweloperzy mają dostęp do metod SDK, oraz dostarcza bezpieczny, audytowalny mechanizm uwierzytelniania oparty na tokenach.

<div class="intro-cta">
<a class="intro-cta__primary" href="/pl/guide/quick-start/">Zacznij tutaj</a>
<a class="intro-cta__secondary" href="/pl/reference/rest-api/">REST API Reference</a>
</div>

<script>
  import IntroCards from '$lib/IntroCards.svelte'

  const howCards = [
    { icon: 'shield', title: 'Uwierzytelnij', desc: 'Deweloperzy logują się przez Keycloak, zewnętrzny urząd JWT.' },
    { icon: 'key', title: 'Wydaj', desc: 'AuthKit wydaje podpisane tokeny deweloperskie (JWT) oparte na magazynie kluczy Core.' },
    { icon: 'sync', title: 'Korzystaj', desc: 'Klienci SDK żądają, weryfikują i zarządzają tokenami przez endpointy REST i gRPC.' },
  ]
  const transportCards = [
    { icon: 'code', title: 'REST API', desc: 'Tokeny deweloperskie przez HTTPS ze szczegółami błędów RFC 7807.', href: '/pl/reference/rest-api/' },
    { icon: 'sync', title: 'gRPC', desc: 'Wydajne kontraktowe endpointy do komunikacji backend-backend.', href: '/pl/reference/grpc/' },
  ]
  const infraCards = [
    { icon: 'shield', title: 'Keycloak', desc: 'Zewnętrzny urząd JWT do logowania deweloperów.', href: '/pl/infrastructure/keycloak/' },
    { icon: 'db', title: 'Marten + PostgreSQL', desc: 'Szyfrowany magazyn kluczy i powiązania tokenów jako trwałe dokumenty.', href: '/pl/infrastructure/marten/' },
    { icon: 'layers', title: 'Wolverine', desc: 'Obsługa komend i zapytań wewnątrz potoku hosta.', href: '/pl/infrastructure/wolverine/' },
    { icon: 'box', title: 'Docker + Kestrel', desc: 'Cały stos jednym plikiem compose.', href: '/pl/infrastructure/docker/' },
  ]
  const pluginCards = [
    { icon: 'key', title: 'DevTokens', desc: 'Wydaje i weryfikuje tokeny deweloperskie do dostępu SDK.', href: '/pl/plugins/devtokens/' },
    { icon: 'terminal', title: 'DevTools', desc: 'Narzędzia hostowane: Swagger z refleksji i utility deweloperskie.', href: '/pl/plugins/devtools/' },
    { icon: 'plug', title: 'Własne wtyczki', desc: 'Opnij własne rozwiązanie o kontrakt IAuthKitPlugin.', href: '/pl/plugins/custom/' },
  ]
</script>

## Jak działa AuthKit

<IntroCards cards={howCards} linkLabel="Czytaj dalej →" />

## Transporty

Wybierz, jak klienci SDK komunikują się z AuthKit.

<IntroCards cards={transportCards} columns={2} linkLabel="Czytaj dalej →" />

## Infrastruktura

AuthKit stoi na sprawdzonych klockach, składanych przez host.

<IntroCards cards={infraCards} columns={2} linkLabel="Czytaj dalej →" />

## Wtyczki i narzędzia

Rozszerzaj zachowanie albo podglądaj działający serwer.

<IntroCards cards={pluginCards} linkLabel="Czytaj dalej →" />

## Szybki start

1. Uruchom usługę AuthKit z Dockerem
2. Skonfiguruj Keycloak
3. Utwórz tokeny deweloperskie do dostępu SDK
4. Używaj tokenów do uwierzytelniania żądań SDK

:::tip[Rekomendowana ścieżka]
Przejdź do przewodnika [Szybki start](/pl/guide/quick-start/), aby ruszyć w kilka minut.
:::

## Architektura

AuthKit składa się z trzech warstw:

### Core

Współdzielona domena, zarządzanie kluczami podpisującymi JWT (generowanie kluczy RSA, szyfrowanie AES, magazyn na dysku), powiązania kluczy tokenów oraz opcje Core.

### Host

Host ASP.NET Core na Kestrelu. Dostarcza Wolverine (obsługa komend/zapytań), Marten (magazyn eventów/dokumentów PostgreSQL), endpointy REST i gRPC, integrację Keycloak, CLI oraz dynamiczne ładowanie wtyczek.

### Plugins

Rozszerzenia odkrywane i ładowane dynamicznie z katalogu `plugins/`. Wtyczki wnoszą usługi, oprogramowanie pośredniczące, health checki oraz schematy bezpieczeństwa OpenAPI poprzez kontrakt `IAuthKitPlugin`.

```text
src/
├── Core/                 # Domena, zarządzanie kluczami, opcje
├── Host/                 # Host webowy, REST/gRPC, CLI, ładowanie wtyczek
│   ├── Configuration/    # Auth, Keycloak, Kestrel, Marten, ServiceDiscovery
│   ├── Grpc/             # Usługi i protos gRPC
│   ├── KeyManagement/    # Endpoint JWKS, inicjalizacja magazynu kluczy
│   ├── Restful/          # Oprogramowanie pośredniczące hosta
│   └── ServiceDiscovery/ # Automatyczna rejestracja DI
└── Plugins/
    ├── Abstractions/     # Kontrakt IAuthKitPlugin
    └── Solutions/        # Implementacje wtyczek (np. DevTokens)
```

## Wbudowane wtyczki

### DevTokens

Wydaje i weryfikuje tokeny deweloperskie do dostępu SDK.

## REST API Reference

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `sdk/developer-tokens` | Utwórz token deweloperski |
| `GET` | `sdk/developer-tokens` | Lista tokenów deweloperskich |
| `GET` | `sdk/developer-tokens/{tokenId}` | Pobierz token po ID |
| `DELETE` | `sdk/developer-tokens/{tokenId}` | Usuń token |
| `POST` | `sdk/tokens/verify` | Zweryfikuj token deweloperski |
| `POST` | `sdk/tokens/{tokenId}/revoke-rotate` | Unieważnij i rotuj token |

## Następne kroki

1. [Szybki start](/pl/guide/quick-start/)
2. [REST API Reference](/pl/reference/rest-api/)
3. [Konfiguracja](/pl/guide/configuration/)
