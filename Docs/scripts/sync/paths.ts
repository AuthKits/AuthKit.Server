import { readdir, readFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

export const docsRoot = join(dirname(fileURLToPath(import.meta.url)), '..', '..')
export const routesRoot = join(docsRoot, 'src', 'routes')
export const contentRoot = join(docsRoot, 'content')
export const adrSource = join(contentRoot, 'en', 'adr')
export const messagesRoot = join(docsRoot, 'i18n', 'messages')
export const settingsPath = join(docsRoot, 'project.inlang', 'settings.json')
export const registryPath = join(docsRoot, 'src', 'lib', 'site-locales.ts')

export interface SyncStats {
  written: number
  removed: string[]
}

export const stats: SyncStats = { written: 0, removed: [] }

export async function readJsonFile(path: string, what: string): Promise<unknown> {
  let raw: string
  try {
    raw = await readFile(path, 'utf8')
  } catch (e) {
    throw new Error(`sync: cannot read ${what} at ${path}: ${(e as Error).message}`)
  }
  try {
    return JSON.parse(raw)
  } catch (e) {
    throw new Error(`sync: invalid JSON in ${what} at ${path}: ${(e as Error).message}`)
  }
}

export async function countPages(dir: string): Promise<number> {
  let n = 0
  let entries
  try {
    entries = await readdir(dir, { withFileTypes: true })
  } catch {
    return 0
  }
  for (const entry of entries) {
    const full = join(dir, entry.name)
    if (entry.isDirectory()) n += await countPages(full)
    else if (entry.name === '+page.md' || entry.name === '+page.svelte') n += 1
  }
  return n
}
