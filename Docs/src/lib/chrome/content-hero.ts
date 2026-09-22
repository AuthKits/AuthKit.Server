// Shared hero builder for content-chrome components.
//
// Both AdrChrome (adr hero*) and SectionChrome (sec-hero*) wrap the page h1
// + lead paragraphs into a hero panel with stat cards and a section nav.
// This module owns that algorithm once (DRY); components only supply their
// class namespace + page specific stats. Styling stays in the components.
import { el, sectionLinks, statCard } from './dom'

export interface HeroNamespace {
  hero: string
  stats: string
  card: string
  num: string
  label: string
  nav: string
}

export interface HeroStat {
  value: string
  label: string
}

/**
 * Move h1 + following lead paragraphs into a hero panel.
 * @returns the hero element, or null when the page has no h1.
 */
export function wrapHero(root: ParentNode, heroClass: string): HTMLDivElement | null {
  const h1 = root.querySelector(':scope > h1')
  if (!h1) return null
  const hero = el('div', heroClass)
  h1.before(hero)
  hero.appendChild(h1)
  let next: ChildNode | null = hero.nextSibling
  while (next && next instanceof HTMLParagraphElement) {
    const cur = next
    next = next.nextSibling
    hero.appendChild(cur)
  }
  return hero
}

/** Append stat cards to a hero. */
export function appendStats(hero: HTMLElement, items: HeroStat[], ns: HeroNamespace): void {
  const stats = el('div', ns.stats)
  for (const item of items) {
    stats.append(statCard(item.value, item.label, { card: ns.card, num: ns.num, labelCls: ns.label }))
  }
  hero.appendChild(stats)
}

/** Append a nav of h2 section links to a hero. No-op without sections. */
export function appendSectionNav(
  hero: HTMLElement,
  root: ParentNode,
  ns: HeroNamespace,
  ariaLabel = '',
): void
{
  const links = sectionLinks(root)
  if (links.length === 0) return
  const nav = el('nav', ns.nav)
  if (ariaLabel) nav.setAttribute('aria-label', ariaLabel)
  for (const { href, text } of links) {
    const a = el('a', '', text)
    a.href = href
    nav.appendChild(a)
  }
  hero.appendChild(nav)
}
