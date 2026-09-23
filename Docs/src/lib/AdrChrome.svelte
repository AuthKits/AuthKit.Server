<script>
  import { page } from '$app/state'
  import { tick, untrack } from 'svelte'
  import { buildRegistryFilter } from './adr/registry-filter'
  import { matchSection } from './adr/section-match'
  import { appendSectionNav, appendStats, wrapHero } from './chrome/content-hero'
  import { chromeLabels, documentLocale } from './chrome/labels'

  let { kicker = 'INDEX', num = null } = $props()

  const NS = {
    hero: 'adr-hero relative mt-5 overflow-hidden rounded-2xl border border-edge2 px-7 pt-8 pb-6 [background:radial-gradient(circle_at_100%_0%,rgb(52_211_153/10%),transparent_45%),radial-gradient(circle_at_0%_100%,rgb(244_63_94/7%),transparent_40%),#0b0b0d]',
    stats: 'adr-hero__stats adr-injected mt-6 flex flex-wrap gap-2.5',
    card: 'flex flex-col gap-0.5 rounded-[10px] border border-edge2 bg-black/35 px-3.5 py-2 whitespace-nowrap',
    num: 'font-mono text-[1.35rem] leading-[1.1] font-bold text-[#fafafa]',
    label: 'font-mono text-[0.68rem] tracking-[0.1em] text-fog uppercase',
    nav: 'adr-hero__nav adr-injected mt-4 flex flex-wrap gap-2 border-t border-edge pt-4',
    link: 'rounded-full border border-edge2 px-3 py-1.5 font-mono text-[0.72rem] tracking-[0.04em] text-mist no-underline transition-colors hover:border-[#52525b] hover:text-paper',
  }

  function unwrap(root) {
    for (const el of root.querySelectorAll('.adr-decision, .adr-rejected, .adr-consequences')) {
      el.replaceWith(...el.childNodes)
    }
    for (const el of root.querySelectorAll('.adr-map, .adr-registry, .adr-steps, .adr-anatomy, .adr-areas, .adr-pro, .adr-con, .adr-flow')) {
      el.classList.remove('adr-map', 'adr-registry', 'adr-steps', 'adr-anatomy', 'adr-areas', 'adr-pro', 'adr-con', 'adr-flow')
    }
    for (const el of root.querySelectorAll('.adr-filter, .adr-filter__empty')) el.remove()
    for (const el of root.querySelectorAll('tr[hidden]')) el.removeAttribute('hidden')
    for (const el of root.querySelectorAll('.adr-injected')) el.remove()
    for (const el of root.querySelectorAll('.adr-hero')) el.replaceWith(...el.childNodes)
  }

  function wrapSections() {
    const root = document.querySelector('.adr-page .content')
    if (!root) return
    unwrap(root)
    for (const h2 of [...root.children].filter(el => el.tagName === 'H2')) {
      const href = h2.querySelector(':scope > a')?.getAttribute('href') ?? ''
      const cls = matchSection(href, documentLocale())
      if (!cls) continue
      const wrap = document.createElement('div')
      wrap.className = cls
      h2.before(wrap)
      wrap.appendChild(h2)
      let next = wrap.nextSibling
      while (next && next.tagName !== 'H2') {
        const cur = next
        next = next.nextSibling
        wrap.appendChild(cur)
      }
    }
  }

  // Index page: classify tables/lists/blocks by structure (language-agnostic,
  // so en + pl share the paint below). Idempotent — unwrap() strips first.
  function decorateIndex() {
    const root = document.querySelector('.adr-page.adr-index .content')
    if (!root) return
    for (const table of root.querySelectorAll('table')) {
      const cols = table.querySelectorAll('thead th').length
      table.classList.add(cols === 2 ? 'adr-map' : 'adr-registry')
      if (cols !== 2) buildFilter(table)
    }
    for (const ol of root.querySelectorAll('ol')) ol.classList.add('adr-steps')
    for (const ul of root.querySelectorAll('ul'))
    {
      const firstLi = ul.querySelector(':scope > li')
      const first = firstLi?.firstChild
      if (first && first.nodeType === 1 && first.tagName === 'CODE')
      {
        ul.classList.add('adr-anatomy')
        for (const li of ul.querySelectorAll(':scope > li'))
        {
          for (const node of [...li.childNodes])
          {
            if (node.nodeType !== 3) continue
            const stripped = node.textContent.replace(/^:\s*/, '')
            if (stripped !== node.textContent) node.textContent = stripped
          }
        }
      }
    }
    for (const pre of root.querySelectorAll('pre'))
    {
      if (/Core.*Host.*Plugins/s.test(pre.textContent ?? '')) pre.classList.add('adr-flow')
    }
    for (const h2 of root.querySelectorAll('h2'))
    {
      const text = h2.textContent ?? ''
      if (/czyta|read/i.test(text))
        {
        let next = h2.nextElementSibling
        while (next && next.tagName !== 'H2')
        {
          if (next.tagName === 'UL' && !next.classList.contains('adr-anatomy')) {
            next.classList.add('adr-areas')
            break
          }
          next = next.nextElementSibling
        }
      }
      if (!/kiedy|when/i.test(text)) continue
      const lists = []
      let sib = h2.nextElementSibling
      while (sib && sib.tagName !== 'H2') {
        if (sib.tagName === 'UL') lists.push(sib)
        sib = sib.nextElementSibling
      }
      if (lists[0]) lists[0].classList.add('adr-pro')
      if (lists[1]) lists[1].classList.add('adr-con')
    }
    buildHero(root)
  }

  // Filter bar above the ADR registry: text search + area pills, derived
  // from the table itself (language-agnostic). Rebuilt on each run.
  // Implementation lives in ./adr/registry-filter here only wiring.
  function buildFilter(table) {
    const labels = chromeLabels(documentLocale())
    buildRegistryFilter(table, { search: labels.search, all: labels.all, shown: labels.shown, empty: labels.empty })
  }

  // Hero for the ADR index: wraps h1 + lead paragraphs into a panel with
  // live stats (decision/area count, latest entry) and quick links to the
  // page sections. Built from the DOM, so en + pl share it.
  function buildHero(root)
  {
    const hero = wrapHero(root, NS.hero)
    if (!hero) return
    const labels = chromeLabels(documentLocale())
    const decisions = root.querySelectorAll('table.adr-registry tbody tr').length
    const areaCount = root.querySelectorAll('table.adr-map tbody tr').length
    const dates = [...root.querySelectorAll('table.adr-registry tbody tr td:nth-child(5)')]
      .map(td => td.textContent.trim()).filter(Boolean).sort()
    const items = [
      { value: String(decisions), label: labels.decisions },
      { value: String(areaCount), label: labels.areas },
    ]
    if (dates.length) items.push({ value: dates[dates.length - 1], label: labels.latestEntry })
    appendStats(hero, items, NS)
    appendSectionNav(hero, root, NS, labels.registrySections)
  }

  $effect(() => {
    page.url.pathname
    untrack(() => tick().then(() => { wrapSections(); decorateIndex() }))
  })
</script>

<div class="relative mb-2 flex items-center justify-between gap-4 overflow-hidden border-b border-edge pt-1.5 pb-4" aria-hidden="false">
  <span class="inline-flex items-center gap-2.5 font-mono text-[0.68rem] tracking-[0.16em] whitespace-nowrap text-fog"><span class="h-3 w-[11px] shrink-0 bg-[#52525b] [clip-path:polygon(50%_0,93%_25%,93%_75%,50%_100%,7%_75%,7%_25%)]"></span>ADR&nbsp;&nbsp;/&nbsp;&nbsp;{kicker}</span>
  <span class="overflow-hidden font-mono text-[2.6rem] leading-none font-bold tracking-[0.02em] text-ellipsis whitespace-nowrap text-transparent [-webkit-text-stroke:1px_#26262b] select-none">{num ?? 'ADR'}</span>
</div>

<style>
  /* ---- theme content restyle, scoped to ADR pages ---- */
  :global(.adr-page .content) {
    counter-reset: adr-sec;
  }
  :global(.adr-page .content > p:first-child:has(a)) {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
    align-items: center;
    margin: 1.4rem 0 0;
  }
  :global(.adr-page .content > p:first-child:has(a) a) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.04em;
    color: #a1a1aa;
    text-decoration: none;
    border: 1px solid #2a2a2e;
    border-radius: 999px;
    padding: 0.32rem 0.8rem;
    transition: color 0.15s ease, border-color 0.15s ease;
  }
  :global(.adr-page .content > p:first-child:has(a) a:hover) {
    color: #f4f4f5;
    border-color: #52525b;
  }
  :global(.adr-page .content > p:first-child:has(a) a[href='']) {
    color: #52525b;
    border-color: #1d1d20;
    pointer-events: none;
  }
  :global(.adr-page .content h1) {
    font-size: clamp(1.7rem, 4vw, 2.4rem);
    font-weight: 650;
    letter-spacing: -0.03em;
    line-height: 1.12;
    color: #fafafa;
    margin: 2.2rem 0 0.6rem;
  }
  :global(.adr-page .content h1 a) {
    text-decoration: none;
  }
  :global(.adr-page .content h1 svg),
  :global(.adr-page .content h2 svg),
  :global(.adr-page .content h3 svg) {
    width: 0.7em;
    height: 0.7em;
    color: #52525b;
  }
  :global(.adr-page .content h2) {
    counter-increment: adr-sec;
    font-size: 1.2rem;
    font-weight: 650;
    letter-spacing: -0.02em;
    color: #f4f4f5;
    margin: 2.6rem 0 0.7rem;
    padding-top: 1.3rem;
    border-top: 1px solid #1d1d20;
  }
  :global(.adr-page .content h2::before) {
    content: counter(adr-sec, decimal-leading-zero);
    display: inline-block;
    margin-right: 0.7rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    font-weight: 400;
    letter-spacing: 0.1em;
    color: #52525b;
    vertical-align: 0.18em;
  }
  :global(.adr-page .content h2 + p) {
    color: #d4d4d8;
  }
  :global(.adr-page .content h3) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.74rem;
    font-weight: 600;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: #8e8e93;
    margin: 2rem 0 0.6rem;
  }
  :global(.adr-page .content ul) {
    display: grid;
    gap: 0.45rem;
    padding-left: 1.1rem;
  }
  :global(.adr-page .content ul ul) {
    margin-top: 0.45rem;
  }
  :global(.adr-page .content li::marker) {
    content: '–  ';
    color: #52525b;
  }

  /* ---- section personalities (wrappers injected on mount, see above) ---- */
  :global(.adr-page .adr-decision) {
    margin-top: 2.2rem;
    padding: 1.3rem 1.4rem 1.4rem;
    background: #0b0b0d;
    border: 1px solid #2a2a2e;
    border-radius: 14px;
  }
  :global(.adr-page .adr-decision > h2:first-child) {
    margin-top: 0;
    padding-top: 0;
    border-top: 0;
  }
  :global(.adr-page .adr-decision li::marker) {
    content: '+  ';
    color: #a1a1aa;
  }
  :global(.adr-page .adr-rejected li::marker) {
    content: '✕  ';
    color: #f87171;
  }
  :global(.adr-page .adr-consequences li::marker) {
    content: '→  ';
    color: #d97706;
  }
  :global(.adr-page .content p),
  :global(.adr-page .content li) {
    color: #a1a1aa;
    font-size: 0.95rem;
    line-height: 1.75;
  }
  :global(.adr-page .content li) {
    margin: 0;
  }
  :global(.adr-page .content li::marker) {
    color: #52525b;
  }
  :global(.adr-page .content strong) {
    color: #f4f4f5;
    font-weight: 600;
  }
  :global(.adr-page .content a) {
    color: #f4f4f5;
    text-decoration: underline;
    text-decoration-color: #52525b;
    text-underline-offset: 3px;
  }
  :global(.adr-page .content a:hover) {
    text-decoration-color: #f4f4f5;
  }
  :global(.adr-page .content a[href='']) {
    color: #52525b;
    text-decoration: none;
    pointer-events: none;
  }
  :global(.adr-page .content code) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.82em;
    color: #e4e4e7;
    background: #232326;
    border-radius: 6px;
    padding: 0.15em 0.4em;
  }
  :global(.adr-page .content pre code) {
    background: transparent;
    padding: 0;
    border-radius: 0;
    font-size: inherit;
  }
  :global(.adr-page .content pre) {
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 10px;
    padding: 1rem 1.1rem;
  }
  :global(.adr-page .content blockquote) {
    border-left: 2px solid #3f3f46;
    padding-left: 1rem;
    margin-left: 0;
    color: #8e8e93;
  }
  :global(.adr-page .content table) {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.88rem;
  }
  :global(.adr-page .content th),
  :global(.adr-page .content td) {
    text-align: left;
    padding: 0.6rem 0.8rem;
    border-bottom: 1px solid #1d1d20;
  }
  :global(.adr-page .content th) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: #fafafa;
    font-weight: 600;
  }
  :global(.adr-page .content td) {
    color: #a1a1aa;
  }
  :global(.adr-page .content hr) {
    border: 0;
    border-top: 1px solid #1d1d20;
    margin: 2.2rem 0;
  }
  :global(.adr-page .content em) {
    color: #8e8e93;
  }

  /* ---- Related links: card list ---- */
  :global(.adr-page .content h2:has(> a[href='#Related'], > a[href='#Powiązane']) + ul) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
  }
  :global(.adr-page .content h2:has(> a[href='#Related'], > a[href='#Powiązane']) + ul > li) {
    display: flex;
    align-items: baseline;
    gap: 0.7rem;
    margin: 0;
    padding: 0.6rem 0.9rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 10px;
    font-size: 0.85rem;
    color: #8e8e93;
    transition: border-color 0.15s ease;
  }
  :global(.adr-page .content h2:has(> a[href='#Related'], > a[href='#Powiązane']) + ul > li:hover) {
    border-color: #2a2a2e;
  }
  :global(.adr-page .content h2:has(> a[href='#Related'], > a[href='#Powiązane']) + ul > li::marker) {
    content: '';
  }
  :global(.adr-page .content h2:has(> a[href='#Related'], > a[href='#Powiązane']) + ul > li > a) {
    flex-shrink: 0;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.04em;
    color: #f4f4f5;
    text-decoration: none;
    border: 1px solid #2a2a2e;
    border-radius: 6px;
    padding: 0.2rem 0.5rem;
    background: #131316;
  }

  /* ---- bottom page nav (ADR Home | Category Index | Previous | Next) ---- */
  :global(.adr-page .content > p:last-of-type:has(a)) {
    display: flex;
    flex-wrap: wrap;
    gap: 0.45rem;
    align-items: center;
    margin: 2.5rem 0 0;
    padding-top: 1.4rem;
    border-top: 1px solid #1d1d20;
  }
  :global(.adr-page .content > p:last-of-type:has(a) a) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.04em;
    color: #a1a1aa;
    text-decoration: none;
    border: 1px solid #2a2a2e;
    border-radius: 999px;
    padding: 0.32rem 0.8rem;
    transition: color 0.15s ease, border-color 0.15s ease;
  }
  :global(.adr-page .content > p:last-of-type:has(a) a:hover) {
    color: #f4f4f5;
    border-color: #52525b;
  }
  :global(.adr-page .content > p:last-of-type:has(a) a[href='']) {
    color: #52525b;
    border-color: #1d1d20;
    pointer-events: none;
  }

  /* ---- ADR meta block: status line + Tag/Date/Scope card (detail pages only) ---- */
  :global(.adr-page:not(.adr-index) .content h1 + p) {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    margin: 0.9rem 0 1.4rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: #8e8e93;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p::before) {
    content: '';
    width: 8px;
    height: 8px;
    flex-shrink: 0;
    border-radius: 999px;
    background: #34d399;
    box-shadow: 0 0 12px rgba(52, 211, 153, 0.6);
  }
  :global(.adr-page:not(.adr-index) .content h1 + p em) {
    font-style: normal;
    color: #636366;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p + p),
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p),
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p + p) {
    display: flex;
    align-items: baseline;
    gap: 0.8rem;
    margin: 0;
    padding: 0.55rem 1rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.78rem;
    color: #d4d4d8;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p + p) {
    border-radius: 10px 10px 0 0;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p),
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p + p) {
    border-top: 0;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p + p) {
    border-radius: 0 0 10px 10px;
  }
  :global(.adr-page:not(.adr-index) .content h1 + p + p strong),
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p strong),
  :global(.adr-page:not(.adr-index) .content h1 + p + p + p + p strong) {
    min-width: 3.4rem;
    flex-shrink: 0;
    font-weight: 400;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    font-size: 0.68rem;
    color: #636366;
  }

  /* ---- index page: hero lead ---- */
  :global(.adr-page.adr-index .content h1 + p),
  :global(.adr-page.adr-index .content h1 + p + p) {
    color: #d4d4d8;
    font-size: 0.95rem;
    line-height: 1.75;
    max-width: 68ch;
  }

  /* ---- index page: registry + map tables become cards ---- */
  :global(.adr-page.adr-index .content [hidden]) {
    display: none !important;
  }
  :global(.adr-page.adr-index .content table.adr-registry),
  :global(.adr-page.adr-index .content table.adr-map) {
    display: grid;
    gap: 0.5rem;
    margin: 1rem 0 0;
  }
  :global(.adr-page.adr-index .content table.adr-registry thead),
  :global(.adr-page.adr-index .content table.adr-map thead) {
    display: none;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody),
  :global(.adr-page.adr-index .content table.adr-map tbody) {
    display: grid;
    gap: 0.5rem;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody tr),
  :global(.adr-page.adr-index .content table.adr-map tbody tr) {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 0.35rem 0.7rem;
    padding: 0.7rem 0.9rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 10px;
    transition: border-color 0.15s ease;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody tr:hover),
  :global(.adr-page.adr-index .content table.adr-map tbody tr:hover) {
    border-color: #3f3f46;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody td),
  :global(.adr-page.adr-index .content table.adr-map tbody td) {
    border: 0;
    padding: 0;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody td:first-child a) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.04em;
    color: #f4f4f5;
    text-decoration: none;
    border: 1px solid #2a2a2e;
    border-radius: 6px;
    padding: 0.2rem 0.5rem;
    background: #131316;
    white-space: nowrap;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody td:nth-child(2)) {
    flex: 1 1 16rem;
    color: #d4d4d8;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody td:nth-child(n + 3)) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.7rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: #8e8e93;
  }
  :global(.adr-page.adr-index .content table.adr-registry tbody td:nth-child(4)::before) {
    content: '';
    display: inline-block;
    width: 7px;
    height: 7px;
    border-radius: 999px;
    background: #34d399;
    box-shadow: 0 0 10px rgba(52, 211, 153, 0.6);
    margin-right: 0.45rem;
    vertical-align: 0.08em;
  }
  :global(.adr-page.adr-index .content table.adr-map tbody td:first-child) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: #f4f4f5;
    border: 1px solid #2a2a2e;
    border-radius: 6px;
    padding: 0.2rem 0.5rem;
    background: #131316;
    white-space: nowrap;
  }
  :global(.adr-page.adr-index .content table.adr-map tbody td:nth-child(2)) {
    flex: 1 1 16rem;
    color: #a1a1aa;
    font-size: 0.85rem;
  }

  /* ---- index page: steps (ordered list) ---- */
  :global(.adr-page.adr-index .content ol.adr-steps) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
    counter-reset: adr-step;
  }
  :global(.adr-page.adr-index .content ol.adr-steps > li) {
    position: relative;
    counter-increment: adr-step;
    display: block;
    margin: 0;
    padding: 0.65rem 0.9rem 0.65rem 3.2rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 10px;
    color: #d4d4d8;
  }
  :global(.adr-page.adr-index .content ol.adr-steps > li::marker) {
    content: '';
  }
  :global(.adr-page.adr-index .content ol.adr-steps > li::before) {
    content: counter(adr-step, decimal-leading-zero);
    position: absolute;
    left: 0.9rem;
    top: 0.85rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    color: #8e8e93;
  }

  /* ---- index page: ADR anatomy (Context/Problem/… term cards) ---- */
  :global(.adr-page.adr-index .content ul.adr-anatomy) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
    counter-reset: adr-term;
  }
  :global(.adr-page.adr-index .content ul.adr-anatomy > li) {
    position: relative;
    counter-increment: adr-term;
    margin: 0;
    padding: 0.7rem 0.9rem 0.7rem 3.2rem;
    background: #0b0b0d;
    border: 1px solid #2a2a2e;
    border-radius: 10px;
    color: #a1a1aa;
  }
  :global(.adr-page.adr-index .content ul.adr-anatomy > li::marker) {
    content: '';
  }
  :global(.adr-page.adr-index .content ul.adr-anatomy > li::before) {
    content: counter(adr-term, decimal-leading-zero);
    position: absolute;
    left: 0.9rem;
    top: 0.85rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    color: #52525b;
  }
  :global(.adr-page.adr-index .content ul.adr-anatomy > li > code:first-child) {
    display: table;
    margin-bottom: 0.35rem;
    font-size: 0.78rem;
    font-weight: 600;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: #fafafa;
    background: #1c1c1f;
    border: 1px solid #3f3f46;
    border-radius: 6px;
    padding: 0.25em 0.6em;
  }

  /* ---- index page: area picker cards (if change touches…) ---- */
  :global(.adr-page.adr-index .content ul.adr-areas) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
  }
  :global(.adr-page.adr-index .content ul.adr-areas > li) {
    margin: 0;
    padding: 0.7rem 0.9rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-left: 2px solid #3f3f46;
    border-radius: 0 10px 10px 0;
    color: #a1a1aa;
    transition: border-color 0.15s ease;
  }
  :global(.adr-page.adr-index .content ul.adr-areas > li:hover) {
    border-color: #3f3f46;
    border-left-color: #34d399;
  }
  :global(.adr-page.adr-index .content ul.adr-areas > li::marker) {
    content: '';
  }

  /* index page: pro / con lists  */
  :global(.adr-page.adr-index .content ul.adr-pro > li::marker) {
    content: '+  ';
    color: #34d399;
  }
  :global(.adr-page.adr-index .content ul.adr-con > li::marker) {
    content: '✕  ';
    color: #f87171;
  }

  /* index page: architecture flow banner */
  :global(.adr-page.adr-index .content pre.adr-flow) {
    text-align: center;
    font-size: 1rem;
    letter-spacing: 0.06em;
    padding: 1.1rem;
  }

  /* index page: registry filter bar (layout via utilities in registry-filter) */
  :global(.adr-page.adr-index .content [hidden]) {
    display: none !important;
  }

  /* index page: hero typography (panel layout via utilities in NS) */
  :global(.adr-page.adr-index .content .adr-hero h1) {
    margin: 0;
    font-size: clamp(1.9rem, 4.5vw, 2.7rem);
  }
  :global(.adr-page.adr-index .content .adr-hero > p) {
    max-width: 70ch;
    color: #d4d4d8;
  }
  :global(.adr-page.adr-index .content .adr-hero > p:first-of-type) {
    font-size: 1.02rem;
    color: #e4e4e7;
  }
</style>
