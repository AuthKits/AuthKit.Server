# Własne wtyczki

Każde zachowanie w AuthKit, które nie jest domeną Core ani hydrauliką hosta, przyjeżdża jako wtyczka łącznie z wbudowanymi [DevTokens](/pl/plugins/devtokens/) i [DevTools](/pl/plugins/devtools/). Napisanie własnej idzie tą samą ścieżką, co wbudowane: zaimplementuj jeden kontrakt, zadeklaruj metadane, wrzuć assembly do katalogu. Resztą zajmuje się host.

## Dlaczego wtyczki zamiast forków

> Centrum kosztów AuthKit to kontrakty między warstwami, nie pojedyncze klasy. Fork hosta pod własne zachowanie splątałby Twój kod z naszym przy każdej aktualizacji. Kontrakt wtyczek odwraca to: host wystawia stabilne sloty, a Twoje assembly je wypełnia bez referencji do wnętrzności hosta. Kiedy host ewoluuje, to kontrakt stoi w miejscu.

## Kontrakt

`IAuthKitPlugin` to interfejs partial jeden facet na zagadnienie, pogrupowane w pliki `IAuthKitPlugin*.cs`: bazowa tożsamość i akcesory metadanych, wymagania hosta, konfiguracja, health, potok, bezpieczeństwo i haki cyklu życia. Implementujesz facety, których potrzebujesz reszta przychodzi z defaultami wyliczonymi z metadanych.

W praktyce minimalna wtyczka wygląda jak dołączony przykład:

```csharp
[PluginMetadata(
    id: "acme.audit",
    version: "1.0.0",
    tags: ["audit"],
    dependsOn: [],
    capabilities: ["audit"],
    name: "Audit",
    displayName: "Audit Trail",
    description: "Appends audit entries for token operations.",
    author: "Acme",
    license: "MIT")]
public sealed class AuditPlugin : IAuthKitPlugin
{
    public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
    {
        // register plugin services here and only here
    }
}
```

Dwie reguły trzymają model w ryzach:

- **Trzymaj się kontraktu** integrację z hostem ogranicz do tego, co wystawia kontrakt sięgniesz poza, a pęknięcie jest Twoje.
- **Rejestruj w ConfigureServices** każdą własną zależność rejestruj w `ConfigureServices` host nigdy nie podpina Twoich wnętrzności za Ciebie.

## Metadane są nośne

`[PluginMetadata]` to nie dekoracja. Host czyta je przed załadowaniem czegokolwiek i używa do:

- **tożsamości** `Id` musi być niepuste, stabilne między restartami i unikalne w hoście (format i unikalność są walidowane przed aktywacją) `Version` to prawdziwy SemVer z parsowaniem i precedencją
- **prezentacji** `DisplayName ?? Name` plus `Description` lądują w outputcie startowym, diagnostyce i UI
- **możliwości** case insensitive nazwy ficzerów (`plugin.Supports("auth")`) do checków przed aktywacją i walidacji spójności po załadowaniu
- **tagów** case sensitive klasyfikacja do filtrowania w katalogach i UI.

Klasa wtyczki bez atrybutu pada szybko z `InvalidOperationException` głośno przy starcie zamiast tajemniczo w runtime.

:::warning[Nie omijaj kontraktu]
Sięganie poza kontrakt (refleksja we wnętrzności hosta, prywatne API) to jedyny sposób, żeby unieważnić stabilność, którą kupuje model wtyczek. Jeśli brakuje Ci slotu, rozszerz kontrakt zamiast go obchodzić.
:::

## Z katalogu do działającego kodu

Wrzucenie assembly do konfigurowanego katalogu startuje potok (zobacz [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) i [ADR-010](/pl/adr/010-plugin-loading-from-directory/)):

1. **Odkrycie** host skanuje katalog przy starcie nic nie wymaga referencji hosta ani rekompilacji.
2. **Manifest i bramki** metadane lądują w manifeście. `IsEnabled: false` pomija wtyczkę przed ładowaniem (żaden check spójności dla niej nie leci) przyjęte manifesty przechodzą walidację formatu, unikalności i spójności.
3. **Kolejność aktywacji** najpierw zależności (sortowanie topologiczne bije wszystko), potem `Priority` rosnąco wśród gotowych. Niższe liczby startują wcześniej default to `0`.
4. **Konfiguracja** każda wtyczka dostaje swoją zakresową sekcję konfiguracji plus dostęp do buildera hosta przez kontekst konfiguracji wtyczek ([ADR-022](/pl/adr/022-plugin-configuration-context-and-builder/)).
5. **Potok i cykl życia** endpointy i middleware wpinają się przez jawne haki ([ADR-023](/pl/adr/023-plugin-application-pipeline-hooks/)) start i stop podążają za standardowym cyklem życia hosta .NET ([ADR-024](/pl/adr/024-plugin-lifecycle-and-hosted-services/)).
6. **Health i OpenAPI** strukturalne health checki ([ADR-029](/pl/adr/029-structured-plugin-health-contract/)) i schematy bezpieczeństwa płyną do monitoringu i Swaggera DevTools automatycznie.

## Zacznij od przykładu

:::tip[Skopiuj, nie zaczynaj od zera]
`src/Plugins/Solutions/ExamplePlugin/` to minimalna wtyczka powitalna (`authkit.example.ExampleGreeter` przez gRPC) ćwicząca dokładnie tę ścieżkę: metadane, odkrycie, konfiguracja, endpoint. Skopiuj katalog, zmień nazwę id i rośnij z czegoś, co już się ładuje.
:::

## Serwisy gRPC

Wtyczki nie ograniczają się do REST: wtyczka może wystawiać własne serwisy gRPC obok hostowych, mapowane przez to samo odkrywanie. Przykładowa wtyczka robi dokładnie to z `ExampleGreeter/SayHello` zobacz [powierzchnię gRPC](/pl/reference/grpc/) i [serwisy wtyczek](/pl/reference/grpc/#Serwisy-wtyczek).
