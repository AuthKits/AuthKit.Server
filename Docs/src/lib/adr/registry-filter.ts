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

  const bar = el('div', 'adr-filter')
  const input = el('input', 'adr-filter__search')
  input.type = 'search'
  input.placeholder = labels.search
  input.setAttribute('aria-label', labels.search)

  const pills = el('div', 'adr-filter__pills')
  pills.setAttribute('role', 'group')
  let area = ''
  const buttons: HTMLButtonElement[] = []
  for (const name of ['', ...areas])
  {
    const b = el('button', 'adr-filter__pill', name || labels.all)
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

  const count = el('span', 'adr-filter__count')
  count.setAttribute('aria-live', 'polite')
  const empty = el('p', 'adr-filter__empty', labels.empty)
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
