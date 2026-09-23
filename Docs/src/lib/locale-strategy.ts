import {
  defineCustomClientStrategy,
  defineCustomServerStrategy,
} from './paraglide/runtime'
import { siteLocales } from './site-locales.ts'

// SveltePress serves locales from physical URL prefixes (e.g. '/en/',
// '/pl/') defined in the generated registry (src/lib/site-locales.ts).
// The Paraglide "url" strategy is OFF on purpose (it would de-localize URLs
// and fight SveltePress routing), so this custom strategy mirrors the
// SveltePress prefix into the Paraglide locale. Bare '/' is the redirect
// page — treated as the default locale.

const STRATEGY_NAME = 'custom-sveltepress' as const

export function localeFromPathname(pathname: string): string {
  const hit = siteLocales.find(
    locale => pathname === locale.prefix.slice(0, -1) || pathname.startsWith(locale.prefix),
  )
  return hit?.code ?? siteLocales[0].code
}

defineCustomServerStrategy(STRATEGY_NAME, {
  getLocale: (request) => localeFromPathname(new URL(request.url).pathname),
})

defineCustomClientStrategy(STRATEGY_NAME, {
  getLocale: () => localeFromPathname(window.location.pathname),
  setLocale: (locale) => {
    document.cookie = `PARAGLIDE_LOCALE=${locale}; path=/; max-age=34560000; SameSite=Lax`
  },
})
