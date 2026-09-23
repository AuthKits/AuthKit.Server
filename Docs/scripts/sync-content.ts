// Thin orchestrator — logic lives in ./sync/*. All locale route dirs are
// generated (sources: content/); adding a language =
// dropping i18n/messages/<code>.json and running `npm run sync`.
import { readdir, rm, stat } from 'node:fs/promises'
import { join } from 'node:path'
import { validateAdrRegistry } from './sync/adr.ts'
import { writeAdrLayout, writeLandingWrapper, writeRegistry, writeSectionLayout } from './sync/layouts.ts'
import { discoverLocales } from './sync/locales.ts'
import { compileParaglide, ensureParaglideSettings, paraglideStale } from './sync/paraglide.ts'
import { adrSource, contentRoot, countPages, docsRoot, routesRoot, stats } from './sync/paths.ts'
import { adrPage, fallbackCopy, fixAdrRelativeLinks, prefixLocaleLinks, syncContentTree, writeRoute } from './sync/routes.ts'
import { ensureVersionManifests } from './sync/versions.ts'

// 1. Locales, translations, registry, version manifests (never destructive).
const locales = await discoverLocales()
const codes = locales.map(l => l.code)

const settingsChanged = await ensureParaglideSettings(locales)
if (settingsChanged || (await paraglideStale(locales))) compileParaglide()
await writeRegistry(locales)
await ensureVersionManifests(locales)

// 2. Fail fast on missing English first sources before wiping anything.
for (const [label, path] of [
  ['Schemas.md', join(contentRoot, 'en', 'schemas.md')],
  ['ADR index', join(adrSource, 'README.md')],
] as const) {
  try {
    await stat(path)
  } catch {
    throw new Error(`sync: missing ${label} at ${path}`)
  }
}

// 3. Locale dirs are fully generated wipe and rebuild. Also drop dirs of
// locales whose messages file was removed (two letter dirs only, so hand
// written sections like guide/ are never touched).
for (const { code } of locales) {
  await rm(join(routesRoot, code), { recursive: true, force: true })
  stats.removed.push(code)
}
for (const entry of await readdir(routesRoot, { withFileTypes: true })) {
  if (!entry.isDirectory() || !/^[a-z]{2}(-[A-Z]{2})?$/.test(entry.name)) continue
  if (codes.includes(entry.name)) continue
  await rm(join(routesRoot, entry.name), { recursive: true, force: true })
  stats.removed.push(`${entry.name} (stale)`)
}
await rm(join(routesRoot, '+page.md'), { force: true })

// 4. English-first sources (content/en/adr/, content/en/schemas.md) land under en/.
const en = locales.find(l => l.code === 'en') ?? locales[0]
const prefixEn = prefixLocaleLinks(en.code)

await writeRoute(join(en.code, 'schemas'), join(contentRoot, 'en', 'schemas.md'), prefixEn)
await writeRoute(join(en.code, 'adr'), join(adrSource, 'README.md'), content =>
  prefixEn(adrPage('/adr/')(content)),
)

for (const entry of await readdir(adrSource, { withFileTypes: true })) {
  if (!entry.isFile() || !entry.name.endsWith('.md') || entry.name === 'README.md') continue
  const slug = entry.name.slice(0, -'.md'.length)
  await writeRoute(join(en.code, 'adr', slug), join(adrSource, entry.name), content =>
    prefixEn(adrPage('/adr/', en.prefix)(content)),
  )
}

// 5. Per locale content trees (content/<code>/ → routes/<code>/).
for (const locale of locales) {
  try {
    await syncContentTree(join(contentRoot, locale.code), locale.code, prefixLocaleLinks(locale.code), codes)
  } catch {
    // No content/<code>/ yet locale gets English fallback below.
  }
}

// 6. Fallbacks + landing wrappers + ADR chrome for every locale.
for (const locale of locales)
{
  if (locale.code !== en.code) await fallbackCopy(join(routesRoot, en.code), join(routesRoot, locale.code))
  await fixAdrRelativeLinks(locale.code)
  await writeLandingWrapper(locale)
  await writeAdrLayout(locale)
  await writeSectionLayout(locale, 'plugins', 'PLUGIN')
  await writeSectionLayout(locale, 'infrastructure', 'INFRA')
}

// 7. Verify: ADR registry integrity, then every locale must end up with
// pages never deploy an empty or broken one.
await validateAdrRegistry(locales)
for (const locale of locales) {
  const count = await countPages(join(routesRoot, locale.code))
  if (count === 0) throw new Error(`sync: locale '${locale.code}' has no pages after sync — aborting`)
}

console.log(`Locales: ${codes.join(', ')}`)
console.log(`Synchronized ${stats.written} Markdown routes.`)
console.log(`Cleared generated route groups: ${stats.removed.join(', ')}`)
