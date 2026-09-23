// Central label provider for content-chrome components (AdrChrome,
// SectionChrome, registry filter). Single source of truth: i18n/messages
// (paraglide) — no hardcoded pl/en ternaries scattered across components.
// Adding a language = adding a `chrome` section to its messages JSON.
import * as m from '$lib/paraglide/messages.js'

export interface ChromeLabels {
  decisions: string
  areas: string
  latestEntry: string
  registrySections: string
  sections: string
  read: string
  search: string
  all: string
  shown: string
  empty: string
}

type MessageFn = (params?: undefined, options?: { locale?: string }) => string

function t(locale: string, key: string): string {
  const fn = (m as unknown as Record<string, MessageFn>)[key]
  return fn?.(undefined, { locale }) ?? key
}

/** Locale code from <html lang> ('en', 'pl', …). Falls back to 'en'. */
export function documentLocale(): string {
  if (typeof document === 'undefined') return 'en'
  return document.documentElement.lang.slice(0, 2) || 'en'
}

/** All chrome labels for a locale. Unknown locales fall back via paraglide. */
export function chromeLabels(locale: string): ChromeLabels {
  return {
    decisions: t(locale, 'chrome.decisions'),
    areas: t(locale, 'chrome.areas'),
    latestEntry: t(locale, 'chrome.latestEntry'),
    registrySections: t(locale, 'chrome.registrySections'),
    sections: t(locale, 'chrome.sections'),
    read: t(locale, 'chrome.read'),
    search: t(locale, 'chrome.search'),
    all: t(locale, 'chrome.all'),
    shown: t(locale, 'chrome.shown'),
    empty: t(locale, 'chrome.empty'),
  }
}
