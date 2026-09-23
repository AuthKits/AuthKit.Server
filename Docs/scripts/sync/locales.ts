import { readdir } from 'node:fs/promises'
import { join } from 'node:path'
import { messagesRoot, readJsonFile } from './paths.ts'

export interface SiteLocale {
  code: string
  prefix: `/${string}/`
  label: string
  short: string
  navThird: 'adr' | 'config'
}

interface LocaleMessages {
  meta?: { label?: string; short?: string; navThird?: string }
}

// ---------------------------------------------------------------------------
// Locale discovery: every i18n/messages/<code>.json defines a locale.
// Adding a language = dropping in one JSON file (+ optional content/<code>/).
// Everything else derives from that — no code changes needed.
// ---------------------------------------------------------------------------
export async function discoverLocales(): Promise<SiteLocale[]> {
  const entries = await readdir(messagesRoot, { withFileTypes: true })
  const locales: SiteLocale[] = []
  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.endsWith('.json')) continue
    const code = entry.name.slice(0, -'.json'.length)
    if (!/^[a-z]{2}(-[A-Z]{2})?$/.test(code)) continue
    const messages = (await readJsonFile(
      join(messagesRoot, entry.name),
      `locale messages for '${code}'`,
    )) as LocaleMessages
    locales.push({
      code,
      prefix: `/${code}/`,
      label: messages.meta?.label ?? code,
      short: messages.meta?.short ?? code.toUpperCase(),
      navThird: messages.meta?.navThird === 'config' ? 'config' : 'adr',
    })
  }
  // Deterministic order (base first, rest by code) so generated files
  // don't churn between machines with different readdir order.
  locales.sort((a, b) => {
    if (a.code === 'en') return b.code === 'en' ? 0 : -1
    if (b.code === 'en') return 1
    return a.code.localeCompare(b.code)
  })
  if (locales.length === 0) throw new Error(`No locales found in ${messagesRoot}`)
  return locales
}

export const codesLike = (locales: SiteLocale[], slug: string): boolean =>
  locales.some(l => l.code === slug)
