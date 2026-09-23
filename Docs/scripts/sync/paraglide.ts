import { execFileSync } from 'node:child_process'
import { readdir, readFile, stat, writeFile } from 'node:fs/promises'
import { join } from 'node:path'
import type { SiteLocale } from './locales.ts'
import { docsRoot, messagesRoot, settingsPath } from './paths.ts'

export async function ensureParaglideSettings(locales: SiteLocale[]): Promise<boolean> {
  const raw = await readFile(settingsPath, 'utf8')
  const settings = JSON.parse(raw)
  const wanted = locales.map(l => l.code)
  const current: string[] = settings.locales ?? []
  const same = current.length === wanted.length && wanted.every(code => current.includes(code))
  if (same) return false
  // Exact match: add new codes, drop removed ones (order: base first).
  const base = wanted.includes('en') ? ['en'] : [wanted[0]]
  settings.locales = [...base, ...wanted.filter(code => !base.includes(code))]
  await writeFile(settingsPath, `${JSON.stringify(settings, null, 2)}\n`)
  return true
}

export async function paraglideStale(locales: SiteLocale[]): Promise<boolean> {
  try {
    const generated = await readdir(join(docsRoot, 'src', 'lib', 'paraglide', 'messages'))
    for (const { code } of locales) {
      if (!generated.some(f => f.startsWith(`${code}.`) || f === `${code}.js`)) return true
    }
    const indexTime = (await stat(join(docsRoot, 'src', 'lib', 'paraglide', 'messages', '_index.js'))).mtimeMs
    for (const { code } of locales) {
      if ((await stat(join(messagesRoot, `${code}.json`))).mtimeMs > indexTime) return true
    }
    return false
  } catch {
    return true
  }
}

export function compileParaglide(): void {
  execFileSync(
    'npx',
    ['@inlang/paraglide-js', 'compile', '--project', './project.inlang', '--outdir', './src/lib/paraglide'],
    { cwd: docsRoot, stdio: 'inherit' },
  )
}
