import { readdir, readFile } from 'node:fs/promises'
import { join } from 'node:path'
import type { SiteLocale } from './locales.ts'
import { adrSource, contentRoot } from './paths.ts'

// ---------------------------------------------------------------------------
// ADR integrity: the registry tables (content/en/adr/README.md, content/<code>/adr/)
// and Prev/Next chains are hand-maintained and rot silently as the count
// grows. Every sync verifies: table IDs unique, every row links an existing
// file, every Prev/Next link resolves. Failures throw — a broken registry
// must never deploy quietly.
// ---------------------------------------------------------------------------
const TABLE_ROW = /\|\s*\[ADR-(\d+)\]\(([^)]+)\)/g
const ADR_LINK = /\]\(((?:\/[a-z]{2})?\/adr\/([^/)\s]+)\/?)\)/g

function slugFrom(href: string): string {
  return href
    .replace(/^\.\//, '')
    .replace(/\.md$/, '')
    .split('/')
    .filter(Boolean)
    .pop() ?? ''
}

async function adrFiles(code: string): Promise<Set<string>> {
  const files = new Set<string>()
  const dirs = [join(adrSource), join(contentRoot, code, 'adr')]
  for (const dir of dirs) {
    let entries
    try {
      entries = await readdir(dir)
    } catch {
      continue
    }
    for (const name of entries) {
      if (name.endsWith('.md') && name !== 'README.md') files.add(name.slice(0, -'.md'.length))
    }
  }
  return files
}

async function readIndex(code: string, en: SiteLocale): Promise<string> {
  const local = join(contentRoot, code, 'adr', 'README.md')
  try {
    return await readFile(local, 'utf8')
  } catch {
    // content/<code>/adr/ is an optional translation overlay — content/en/adr/ is canonical.
    return await readFile(join(adrSource, 'README.md'), 'utf8')
  }
}

export async function validateAdrRegistry(locales: SiteLocale[]): Promise<void> {
  const en = locales.find(l => l.code === 'en') ?? locales[0]
  for (const locale of locales) {
    const files = await adrFiles(locale.code)
    const index = await readIndex(locale.code, en)
    const seen = new Set<string>()
    for (const m of index.matchAll(TABLE_ROW)) {
      const [, id, href] = m
      if (seen.has(id)) throw new Error(`sync: duplicate ADR-${id} in ${locale.code} registry table`)
      seen.add(id)
      const slug = slugFrom(href)
      if (!slug || !files.has(slug)) {
        throw new Error(`sync: ADR-${id} links missing file '${slug}' (${locale.code})`)
      }
    }
    // Prev/Next chains: every /adr/<slug>/ link must resolve (own locale or en fallback).
    const enFiles = await adrFiles(en.code)
    const checkDirs = [join(contentRoot, locale.code, 'adr'), join(adrSource), join(contentRoot, en.code, 'adr')]
    for (const slug of files) {
      let content: string | null = null
      for (const dir of checkDirs) {
        try {
          content = await readFile(join(dir, `${slug}.md`), 'utf8')
          break
        } catch {
          continue
        }
      }
      if (!content) continue
      for (const m of content.matchAll(ADR_LINK)) {
        const target = m[2]
        if (target === 'README') continue
        if (!files.has(target) && !enFiles.has(target)) {
          throw new Error(`sync: ${locale.code}/${slug} links missing ADR page '${target}'`)
        }
      }
    }
  }
}
