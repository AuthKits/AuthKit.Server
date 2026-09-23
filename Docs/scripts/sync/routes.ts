import { cp, mkdir, readdir, readFile, writeFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { routesRoot, stats } from './paths.ts'

export type Transform = (content: string) => string
export type RouteWriter = (route: string, source: string, transform?: Transform) => Promise<void>

export const writeRoute: RouteWriter = async (route, source, transform = value => value) => {
  const destination = join(routesRoot, route, '+page.md')
  await mkdir(dirname(destination), { recursive: true })
  const content = await readFile(source, 'utf8')
  await writeFile(destination, transform(content))
  stats.written += 1
}

// Root absolute markdown links (](/guide/…) belong to the locale being
// synced rewrite them to /<locale>/…. Already-prefixed, asset and
// protocol-relative links pass through.
export const prefixLocaleLinks = (locale: string): Transform => content =>
  content.replaceAll(new RegExp(`(\\]\\()\\/(?!\\/|${locale}\\/|_app\\/)`, 'g'), `$1/${locale}/`)

// ADR sources link relatively (`./slug.md`, `../../README.md`) — rewrite to
// absolute locale routes under the given target section.
const SIBLING_MD_LINK = /\(\.\/([^\s)]+)\.md\)/g

export function adrPage(target: '/adr/', homePrefix?: string): Transform {
  return content => {
    if (homePrefix) content = content.replaceAll('(../../README.md)', `(${homePrefix})`)
    return content
      .replaceAll(SIBLING_MD_LINK, `(${target}$1/)`)
      .replaceAll(`(${target}README/)`, `(${target})`)
  }
}

export async function syncContentTree(
  sourceDirectory: string,
  routePrefix = '',
  transform: Transform = value => value,
  skip: string[] = [],
): Promise<void> {
  for (const entry of await readdir(sourceDirectory, { withFileTypes: true })) {
    const source = join(sourceDirectory, entry.name)
    if (entry.isDirectory()) {
      await syncContentTree(source, join(routePrefix, entry.name), transform, skip)
      continue
    }
    if (!entry.isFile() || !entry.name.endsWith('.md')) continue

    const route = entry.name === 'README.md'
      ? routePrefix
      : join(routePrefix, entry.name.slice(0, -'.md'.length))
    // '' is the hand written redirect page; '<code>' landings are generated
    // wrappers — never from READMEs (avoids route conflicts).
    if (route === '') continue
    if (skip.includes(route)) {
      console.warn(`sync: skipped ${source} — route '${route}' collides with a locale landing`)
      continue
    }
    await writeRoute(route, source, transform)
  }
}

// Post-pass: content trees (e.g. content/<code>/adr/) carry raw relative
// links (`./slug.md`, `../../README.md`) that don't resolve to routes.
// Rewrite them to absolute locale routes under /<locale>/adr/.
export async function fixAdrRelativeLinks(locale: string): Promise<void> {
  const { readFile, writeFile } = await import('node:fs/promises')
  const adrDir = join(routesRoot, locale, 'adr')
  let entries
  try {
    entries = await readdir(adrDir, { withFileTypes: true })
  } catch {
    return
  }
  const fix = adrPage(`/${locale}/adr/` as '/adr/', `/${locale}/adr/`)
  const targets: string[] = []
  for (const entry of entries) {
    if (entry.isFile() && entry.name === '+page.md') targets.push(join(adrDir, entry.name))
    else if (entry.isDirectory()) targets.push(join(adrDir, entry.name, '+page.md'))
  }
  for (const file of targets) {
    try {
      const raw = await readFile(file, 'utf8')
      const fixed = fix(raw)
      if (fixed !== raw) {
        await writeFile(file, fixed)
        stats.written += 1
      }
    } catch {
      // Missing +page.md in a fresh dir — nothing to fix.
    }
  }
}
// so no link ever 404s — translate progressively.
export async function fallbackCopy(fromDir: string, toDir: string): Promise<void> {
  let entries
  try {
    entries = await readdir(fromDir, { withFileTypes: true })
  } catch {
    return
  }
  for (const entry of entries) {
    const from = join(fromDir, entry.name)
    const to = join(toDir, entry.name)
    if (entry.isDirectory()) {
      await fallbackCopy(from, to)
      continue
    }
    if (!entry.isFile() || entry.name !== '+page.md') continue
    try {
      await readFile(to, 'utf8')
    } catch {
      await mkdir(dirname(to), { recursive: true })
      await cp(from, to)
    }
  }
}
