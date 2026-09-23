// ADR registry filter bar (search input + area pills + live count).
// Extracted from AdrChrome: single responsibility is filtering registry
// table. Display strings are injected (Dependency Inversion), table shape
// (ID | title | area) is the only contract language agnostic.
import { el } from '../chrome/dom'

export interface FilterLabels {
  search: string
  all: string
  shown: string
  empty: string
}

/**
 * @param table the registry <table> to filter (bar is inserted before it)
 * @param labels pre translated UI strings
 */
export function buildRegistryFilter(table: HTMLTableElement, labels: FilterLabels): void
{
  table.parentElement?.querySelector(':scope > .adr-filter')?.remove()
  const rows = [...table.querySelectorAll('tbody tr')]
  const areas = [
    ...new Set(
      rows.map(r => (r.children[2]?.textContent ?? '').trim()).filter(Boolean),
    ),
  ]

  const bar = el('div', 'adr-filter mt-4 mb-0.5 flex flex-wrap items-center gap-2.5 rounded-[10px] border border-edge2 bg-panel px-3.5 py-2.5')
  const input = el('input', 'adr-filter__search min-w-0 flex-[1_1_14rem] rounded-lg border border-edge2 bg-raise px-3 py-2 font-inherit text-[0.85rem] text-paper outline-none placeholder:text-faint focus:border-[#52525b]')
  input.type = 'search'
  input.placeholder = labels.search
  input.setAttribute('aria-label', labels.search)

  const pills = el('div', 'adr-filter__pills flex flex-wrap gap-1.5')
  pills.setAttribute('role', 'group')
  let area = ''
  const buttons: HTMLButtonElement[] = []
  for (const name of ['', ...areas]) {
    const b = el(
      'button',
      'adr-filter__pill cursor-pointer rounded-full border border-edge2 bg-transparent px-3 py-1.5 font-mono text-[0.7rem] tracking-[0.06em] text-mist uppercase transition-colors hover:border-[#52525b] hover:text-paper aria-pressed:border-[#52525b] aria-pressed:bg-well aria-pressed:text-[#fafafa]',
      name || labels.all,
    )
    b.type = 'button'
    b.setAttribute('aria-pressed', name === '' ? 'true' : 'false')
    if (name === '') b.classList.add('is-active')
    b.addEventListener('click', () => {
      area = name
      for (const x of buttons) {
        const on = x === b
        x.classList.toggle('is-active', on)
        x.setAttribute('aria-pressed', String(on))
      }
      apply()
    })
    buttons.push(b)
    pills.appendChild(b)
  }

  const count = el('span', 'adr-filter__count ml-auto font-mono text-[0.7rem] tracking-[0.08em] whitespace-nowrap text-faint')
  count.setAttribute('aria-live', 'polite')
  const empty = el('p', 'adr-filter__empty mt-2 rounded-[10px] border border-dashed border-edge2 p-3.5 text-center text-[0.85rem] text-fog', labels.empty)
  empty.hidden = true

  function apply(): void
  {
    const q = input.value.trim().toLowerCase()
    let visible = 0
    for (const r of rows)
    {
      const cells = r.children
      const hay = `${cells[0]?.textContent ?? ''} ${cells[1]?.textContent ?? ''}`.toLowerCase()
      const ok = (!area || cells[2]?.textContent?.trim() === area) && (!q || hay.includes(q))
      ;(r as HTMLTableRowElement).hidden = !ok
      if (ok) visible++
    }
    count.textContent = `${visible} ${labels.shown} ${rows.length}`
    empty.hidden = visible > 0
  }

  input.addEventListener('input', apply)
  bar.append(input, pills, count)
  table.before(bar)
  table.after(empty)
  apply()
}

const LABELS: Record<string, FilterLabels> = {
  en: {
    search: 'Search by ID or title…',
    all: 'All',
    shown: 'of',
    empty: 'No results for the given filters.',
  },
  pl: {
    search: 'Szukaj po ID lub tytule…',
    all: 'Wszystkie',
    shown: 'z',
    empty: 'Brak wyników dla podanych filtrów.',
  },
}

/**
 * Labels for locale code (eg. 'en', 'pl'). Unknown locales fall back
 * to English adding language means adding one dictionary entry.
 */
export function filterLabels(locale: string): FilterLabels {
  return LABELS[locale] ?? LABELS.en
}
