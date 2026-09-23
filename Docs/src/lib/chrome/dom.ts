// Shared DOM primitives for the content-chrome components (AdrChrome,
// SectionChrome). Single responsibility: element creation only — no
// knowledge of ADR vs section pages.

/** Create an element with an optional class name and text content. */
export function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className = '',
  text = '',
): HTMLElementTagNameMap[K]
{
  const node = document.createElement(tag)
  if (className) node.className = className
  if (text) node.textContent = text
  return node
}

export interface StatClasses
{
  card?: string
  num?: string
  labelCls?: string
}

/**
 * A single hero stat card: big numeric value + uppercase label.
 * Class names are injected so ADR and section pages keep their own skin.
 */
export function statCard(value: string, label: string, classes: StatClasses = {}): HTMLDivElement {
  const d = el('div', classes.card ?? '')
  d.append(el('span', classes.num ?? '', value), el('span', classes.labelCls ?? '', label))
  return d
}

export interface SectionLink {
  href: string
  text: string
}

/**
 * Collect top-level h2 headings of a content root into nav links.
 * Returns an empty array when the page has no linkable sections.
 */
export function sectionLinks(root: ParentNode): SectionLink[]
{
  const links: SectionLink[] = []
  for (const h2 of root.querySelectorAll(':scope > h2')) {
    const href = h2.querySelector(':scope > a')?.getAttribute('href')
    const text = (h2.textContent ?? '').trim()
    if (!href || !text) continue
    links.push({ href, text })
  }
  return links
}
