// Per page accent hues for section pages. Single source of truth —
// SectionChrome resolves, CSS consumes via --sec-accent. Open for extension:
// add page key, no component changes needed.
export const SECTION_ACCENTS: Record<string, string> = {
  devtokens: '#34d399',
  devtools: '#38bdf8',
  custom: '#fbbf24',
  plugins: '#a78bfa',
  marten: '#fb923c',
  keycloak: '#60a5fa',
  wolverine: '#f43f5e',
  docker: '#22d3ee',
  infrastructure: '#2dd4bf',
}

const FALLBACKS: Record<string, string> = { INFRA: '#2dd4bf', PLUGIN: '#34d399' }

/** Resolve the accent color for section page. Pure — trivially testable. */
export function resolveAccent(name: string, section: string): string {
  return SECTION_ACCENTS[name.toLowerCase()] ?? FALLBACKS[section] ?? FALLBACKS.PLUGIN
}
