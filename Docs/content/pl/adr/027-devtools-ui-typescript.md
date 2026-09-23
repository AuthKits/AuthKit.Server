[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/026-plugin-authentication-and-authorization-hooks/) | [Następny](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/)

# [ADR-027] Kompiluj interfejs DevTools z modularnego TypeScript do pojedynczego zasobu osadzonego

*2026-09* | Status: accepted

**Tag:** #adr_027

**Date:** 2026-09-16

**Scope:** Plugins.Solutions.DevTools

## Kontekst

Wtyczka DevTools dostarcza webowe UI (Svelte 5 z Tailwindem) do eksploracji serwisów gRPC i wywoływania metod. Deploy takiego UI jako luźnych plików statycznych rozbiłby każde wydanie na dwa artefakty, które mogą się rozjechać: serwer i interfejs do jego inspekcji.

## Problem

Modularna aplikacja TypeScript nie deployuje się sama jako jeden plik. Bez kroku bundlowania UI wymagałoby historii hostingu plików statycznych (ścieżki, cache, dopasowanie wersji) na wierzchu assembly wtyczki, które i tak niesie wszystko inne.

## Decyzja

UI buduje się przez `vite build`, zwija do jednego samowystarczalnego pliku HTML przez `vite-plugin-singlefile` i jest finalizowane do osadzenia przez `scripts/finalize-ui.mjs`. Bezpieczeństwo typów jest egzekwowane z góry przez `svelte-check`. Wynikowy zasób podróżuje wewnątrz assembly wtyczki: jedno assembly, jedno UI, zawsze zgodne z backendem, z którym płynie.

## Odrzucone

- Serwowanie UI jako luźnych plików statycznych obok assembly wtyczki.
- Wersjonowanie UI niezależnie od inspekcjonowanego serwera.

## Konsekwencje

Deploy AuthKit nigdy nie wymaga osobnego kroku plików statycznych, a wersja UI nie może rozjechać się z inspekcjonowanym serwerem. Kosztem jest sprzężenie build-time: zmiany UI wymagają przebudowania assembly wtyczki.

## Powiązane

- [ADR-020](/pl/adr/020-devtools-plugin/) - wtyczka DevTools hostująca UI
- [ADR-009](/pl/adr/009-dynamic-plugin-discovery/) - odkrywanie wtyczek ładujące assembly

[/pl/](/pl/) | [Indeks kategorii](/pl/adr/) | [Poprzedni](/pl/adr/026-plugin-authentication-and-authorization-hooks/) | [Następny](/pl/adr/028-plugin-contract-and-dynamic-loading-architecture/)
