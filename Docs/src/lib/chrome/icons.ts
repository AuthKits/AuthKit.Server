// Shared SVG icon registry for content components (IntroCards, …).
// built-in set is fixed, but consumers extend it via
// registerIcon() without touching this module or its callers.
const ICONS = new Map<string, string>([
  ['shield', '<path d="M12 2l7 4v6c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-4z"/><path d="M9 12l2 2 4-4"/>'],
  ['key', '<circle cx="7.5" cy="15.5" r="3.5"/><path d="M10.5 12.5L21 2"/><path d="M15 8l3 3"/>'],
  ['sync', '<path d="M17 2l4 4-4 4"/><path d="M3 11V9a4 4 0 014-4h14"/><path d="M7 22l-4-4 4-4"/><path d="M21 13v2a4 4 0 01-4 4H3"/>'],
  ['code', '<path d="M16 18l6-6-6-6"/><path d="M8 6l-6 6 6 6"/>'],
  ['db', '<ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/><path d="M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3"/>'],
  ['layers', '<path d="M12 2l9 5-9 5-9-5 9-5z"/><path d="M3 12l9 5 9-5"/><path d="M3 17l9 5 9-5"/>'],
  ['box', '<path d="M21 8l-9-5-9 5v8l9 5 9-5V8z"/><path d="M3 8l9 5 9-5"/><path d="M12 13v8"/>'],
  ['terminal', '<path d="M4 17l6-6-6-6"/><path d="M12 19h8"/>'],
  ['plug', '<path d="M9 7V3"/><path d="M15 7V3"/><path d="M7 7h10v4a5 5 0 01-10 0V7z"/><path d="M12 16v5"/>'],
])

/** Register (or override) an icon. Unknown names render as empty. */
export function registerIcon(name: string, svgInner: string): void {
  ICONS.set(name, svgInner)
}

/** Inner SVG markup for an icon name, or '' when unknown. */
export function getIcon(name: string): string {
  return ICONS.get(name) ?? ''
}
