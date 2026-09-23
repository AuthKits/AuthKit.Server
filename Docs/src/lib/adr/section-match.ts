// Registry matching ADR section headings to wrapper classes.
//
// Heading slugs come from translated markdown (#Decyzja vs #Decision), so the
// source of truth is i18n/messages (`sections.*` per locale) — not this file.
// A new language only adds its slugs to its messages JSON. Custom (non-i18n)
// mappings can still be added at runtime via registerSection().
import * as m from '$lib/paraglide/messages.js'

export interface SectionEntry {
  /** Heading slugs (without '#') for one-off custom mappings. */
  slugs: string[]
  /** Wrapper class applied to the section. */
  cls: string
}

const BUILTINS: { key: string; cls: string }[] = [
  { key: 'sections.decision', cls: 'adr-decision' },
  { key: 'sections.rejected', cls: 'adr-rejected' },
  { key: 'sections.consequences', cls: 'adr-consequences' },
]

const CUSTOM: SectionEntry[] = []

type MessageFn = (params?: undefined, options?: { locale?: string }) => string

function slugFor(locale: string, key: string): string | null {
  const fn = (m as unknown as Record<string, MessageFn>)[key]
  const value = fn?.(undefined, { locale })
  // Paraglide falls back to the key itself when untranslated — not a slug.
  if (!value || value === key) return null
  return value.toLowerCase()
}

/** Register (or override) a custom section mapping. */
export function registerSection(slugs: string[], cls: string): void {
  const norm = slugs.map(s => s.toLowerCase())
  const hit = CUSTOM.findIndex(e => e.cls === cls)
  if (hit >= 0) CUSTOM[hit] = { slugs: norm, cls }
  else CUSTOM.push({ slugs: norm, cls })
}

/**
 * Match an h2 anchor href (e.g. '#Decyzja', '/en/adr/001-x/#Decision')
 * to a wrapper class for the given locale. Case-insensitive,
 * suffix-tolerant. Returns null when nothing matches.
 */
export function matchSection(href: string, locale = 'en'): string | null {
  const slug = href.split('#').pop()?.toLowerCase() ?? ''
  if (!slug) return null
  for (const { key, cls } of BUILTINS) {
    const expected = slugFor(locale, key)
    if (expected && (slug === expected || slug.endsWith(expected))) return cls
  }
  return CUSTOM.find(e => e.slugs.some(s => slug === s || slug.endsWith(s)))?.cls ?? null
}
