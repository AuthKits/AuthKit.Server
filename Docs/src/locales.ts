import type { LocalesConfig } from '@sveltepress/vite'
import { localeMessages } from './lib/locale-messages.ts'
import { siteLocales } from './lib/site-locales.ts'

// Shiki languages used in code fences across content/, ADR/ and Schemas.md.
// The theme default only ships svelte/sh/js/html/ts/md/css/scss,
// so the list must be extended explicitly (it replaces the default).
export const codeLanguages = [
  'svelte',
  'sh',
  'js',
  'html',
  'ts',
  'md',
  'css',
  'scss',
  'bash',
  'csharp',
  'http',
  'json',
  'mermaid',
  'proto',
] as const

// All UI copy for sidebars/navbars comes from i18n/messages/<code>.json
// (docs.* / navbar.*), so a new locale only needs its JSON file.
// Links stay in SveltePress "logical space" (no locale prefix) — the theme
// applies the active locale prefix at render time. Slugs follow the content
// convention: guide/{introduction,quick-start,configuration},
// reference/rest-api, schemas. Missing pages fall back to English via sync.
function messagesFor(code: string): Record<string, Record<string, string>> {
  return localeMessages[code] ?? localeMessages.en
}

function sidebarFor(code: string) {
  const docs = messagesFor(code).docs
  const group = (key: string, fallback: string, items: { title: string; to: string }[]) => ({
    title: docs[key] ?? fallback,
    items,
  })
  const guideGroup = group('guide', 'Guide', [
    { title: docs.introduction, to: '/guide/introduction/' },
    { title: docs.quickStart, to: '/guide/quick-start/' },
    { title: docs.configuration, to: '/guide/configuration/' },
  ])
  const pluginsGroup = group('plugins', 'Plugins', [
    { title: docs.devTokens ?? 'DevTokens', to: '/plugins/devtokens/' },
    { title: docs.devTools ?? 'DevTools', to: '/plugins/devtools/' },
    { title: docs.customPlugins ?? 'Custom plugins', to: '/plugins/custom/' },
  ])
  const referenceGroup = group('reference', 'Reference', [
    { title: docs.restApi, to: '/reference/rest-api/' },
    { title: docs.grpc ?? 'gRPC', to: '/reference/grpc/' },
    { title: docs.schemas, to: '/schemas/' },
  ])
  const infraGroup = group('infrastructure', 'Infrastructure', [
    { title: docs.keycloak ?? 'Keycloak', to: '/infrastructure/keycloak/' },
    { title: docs.marten ?? 'Marten + PostgreSQL', to: '/infrastructure/marten/' },
    { title: docs.wolverine ?? 'Wolverine', to: '/infrastructure/wolverine/' },
    { title: docs.docker ?? 'Docker + Kestrel', to: '/infrastructure/docker/' },
  ])
  const adrGroup = group('adrOverview', 'ADR', [
    { title: docs.adrOverview ?? 'ADR', to: '/adr/' },
  ])
  // Fixed global group order on every sidebar — the active section is
  // highlighted, never reordered, so navigation doesn't jump between pages.
  return {
    '/guide/': [guideGroup, pluginsGroup, infraGroup, adrGroup],
    '/reference/': [referenceGroup, guideGroup, pluginsGroup, infraGroup, adrGroup],
    '/schemas/': [
      group('schemas', 'Schemas', [{ title: docs.schemas, to: '/schemas/' }]),
      guideGroup,
      pluginsGroup,
      infraGroup,
      adrGroup,
    ],
    '/adr/': [guideGroup, pluginsGroup, infraGroup, adrGroup],
    '/plugins/': [guideGroup, pluginsGroup, infraGroup, adrGroup],
    '/infrastructure/': [guideGroup, pluginsGroup, infraGroup, adrGroup],
  }
}

// Kept for backwards compatibility (were imported elsewhere historically).
export const enSidebar = sidebarFor('en')
export const plSidebar = sidebarFor('pl')

function navbarFor(code: string) {
  const messages = messagesFor(code)
  return [{ title: messages.navbar.home, to: `/${code}/` }]
}

export const locales: LocalesConfig = Object.fromEntries(
  siteLocales.map(locale => [
    locale.prefix,
    {
      lang: locale.code,
      label: locale.label,
      theme: {
        highlighter: { languages: [...codeLanguages] },
        navbar: navbarFor(locale.code),
        sidebar: sidebarFor(locale.code),
      },
    },
  ]),
)
