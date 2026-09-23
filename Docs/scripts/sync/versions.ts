import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join } from 'node:path'
import { codesLike } from './locales.ts'
import type { SiteLocale } from './locales.ts'
import { docsRoot } from './paths.ts'

// ---------------------------------------------------------------------------
// Version manifests: every locale needs sveltepress.versions.<code>.json
// (the plugin resolves per-locale manifests by URL prefix; without it the
// theme hides the version selector). The default manifest orchestrates bare
// `versions build`/`validate` across all locales. Created once, never
// overwritten history and current IDs are preserved. Adding a language =
// dropping in messages JSON: the next sync scaffolds its manifest, then run
// `sveltepress versions migrate --site-id <id> --locale <code>`.
// ---------------------------------------------------------------------------
export const DEFAULT_VERSIONS_FILE = 'sveltepress.versions.json'

export interface VersionManifestSkeleton {
  $schema: string
  basePath: string
  current: { id: string; label: string }
  versions: []
  content: { include: string[]; exclude: string[]; shared: string[] }
  artifacts?: { mode: string; siteId: string; store: string; sources: string }
}

export async function ensureVersionManifests(locales: SiteLocale[]): Promise<void> {
  const readJson = async (file: string): Promise<VersionManifestSkeleton | null> => {
    try {
      return JSON.parse(await readFile(join(docsRoot, file), 'utf8'))
    } catch {
      return null
    }
  }
  const created: string[] = []
  const existing = await Promise.all(
    locales.map(l => readJson(`sveltepress.versions.${l.code}.json`)),
  )
  const currentId =
    (await readJson(DEFAULT_VERSIONS_FILE))?.current?.id ??
    existing.find(m => m?.current?.id)?.current?.id ??
    '0.1'
  const siteId =
    (await readJson(DEFAULT_VERSIONS_FILE))?.artifacts?.siteId ?? 'authkit-docs'
  const skeleton = (basePath: string, sources?: string): VersionManifestSkeleton => ({
    $schema: './node_modules/@sveltepress/cli/schema/versions.schema.json',
    basePath,
    current: { id: currentId, label: currentId },
    versions: [],
    content: { include: ['**'], exclude: [], shared: ['$lib/**', 'static/**'] },
    ...(sources
      ? {
          artifacts: {
            mode: 'incremental',
            siteId,
            store: '.sveltepress/version-artifacts',
            sources,
          },
        }
      : {}),
  })
  if (!(await readJson(DEFAULT_VERSIONS_FILE))) {
    await writeFile(
      join(docsRoot, DEFAULT_VERSIONS_FILE),
      JSON.stringify(skeleton('/v'), null, 2) + '\n',
    )
    created.push(DEFAULT_VERSIONS_FILE)
  }
  for (const locale of locales) {
    const file = `sveltepress.versions.${locale.code}.json`
    if (await readJson(file)) continue
    await writeFile(
      join(docsRoot, file),
      JSON.stringify(
        skeleton(`${locale.prefix}v`, `version-deltas-${locale.code}`),
        null,
        2,
      ) + '\n',
    )
    created.push(file)
  }
  // Manifests of removed locales are history — never auto-delete, just warn.
  const dir = await readdir(docsRoot)
  for (const name of dir) {
    const m = /^sveltepress\.versions\.([a-z0-9-]+)\.json$/.exec(name)
    if (m && !codesLike(locales, m[1])) {
      console.warn(
        `sync: stale ${name} — locale '${m[1]}' is gone, kept (history). Remove by hand if intended.`,
      )
    }
  }
  if (created.length > 0) {
    console.log(`sync: scaffolded version manifests: ${created.join(', ')}`)
    console.log(
      `sync: next run \`sveltepress versions migrate --site-id ${siteId} --locale <code>\` for each new locale.`,
    )
  }
}
