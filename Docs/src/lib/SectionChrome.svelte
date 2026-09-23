<script>
  // Slim section chrome above SveltePress content on generated section
  // routes (/plugins/*, /infrastructure/*). Family look with ADR pages,
  // anchored at .doc-section.
  //
  // The h1 + lead paragraphs are wrapped into a hero panel on mount
  // and every client-side navigation (pure CSS cannot group siblings).
  import { page } from '$app/state'
  import { tick, untrack } from 'svelte'
  import { appendSectionNav, appendStats, wrapHero } from './chrome/content-hero'
  import { chromeLabels, documentLocale } from './chrome/labels'
  import { resolveAccent } from './chrome/section-accents'

  let { name = '', section = 'SECTION' } = $props()

  const NS = {
    hero: 'sec-hero relative mt-5 overflow-hidden rounded-2xl border px-7 pt-8 pb-6',
    stats: 'sec-hero__stats sec-injected mt-6 flex flex-wrap gap-2.5',
    card: 'flex flex-col gap-0.5 rounded-[10px] border bg-black/35 px-3.5 py-2 whitespace-nowrap [border-color:color-mix(in_srgb,var(--sec-accent,#34d399)_28%,#2a2a2e)]',
    num: 'font-mono text-[1.35rem] leading-[1.1] font-bold text-[var(--sec-accent,#34d399)]',
    label: 'font-mono text-[0.68rem] tracking-[0.1em] text-fog uppercase',
    nav: 'sec-hero__nav mt-4 flex flex-wrap gap-2 border-t border-edge pt-4',
    link: 'rounded-full border border-edge2 px-3 py-1.5 font-mono text-[0.72rem] tracking-[0.04em] text-mist no-underline transition-colors hover:text-paper hover:border-[var(--sec-accent,#34d399)]',
  }

  function unwrap(root) {
    for (const el of root.querySelectorAll('.sec-hero__nav')) el.remove()
    for (const el of root.querySelectorAll('.sec-injected')) el.remove()
    for (const el of root.querySelectorAll('.sec-hero')) {
      el.replaceWith(...el.childNodes)
    }
    for (const el of root.querySelectorAll('.sec-terms')) {
      el.classList.remove('sec-terms')
    }
  }

  // Term lists: every item starts with a bold lead (`<strong>…</strong>: …`)
  // → term cards. Language-agnostic, same pattern as the ADR anatomy.
  function classifyLists(root) {
    for (const ul of root.querySelectorAll(':scope > ul')) {
      const items = ul.querySelectorAll(':scope > li')
      if (items.length === 0) continue
      const allTerms = [...items].every(li => {
        const first = li.firstChild
        return first && first.nodeType === 1 && first.tagName === 'STRONG'
      })
      if (allTerms) ul.classList.add('sec-terms')
    }
  }

  function applyAccent(root) {
    root.style.setProperty('--sec-accent', resolveAccent(name, section))
  }

  function buildHeroFrom(root) {
    const hero = wrapHero(root, NS.hero)
    if (!hero) return
    const labels = chromeLabels(documentLocale())
    appendSectionNav(hero, root, NS)
    const h2s = root.querySelectorAll(':scope > h2').length
    const words = (root.textContent ?? '').trim().split(/\s+/).length
    const mins = Math.max(1, Math.round(words / 200))
    appendStats(hero, [
      { value: String(h2s), label: labels.sections },
      { value: `${mins} min`, label: labels.read },
    ], NS)
  }

  $effect(() => {
    page.url.pathname
    untrack(() => tick().then(() => {
      const root = document.querySelector('.doc-section .content')
      if (!root) return
      unwrap(root)
      applyAccent(root)
      buildHeroFrom(root)
      classifyLists(root)
    }))
  })
</script>

<div class="relative mb-2 flex items-center justify-between gap-4 overflow-hidden border-b border-edge pt-1.5 pb-4" aria-hidden="false">
  <span class="inline-flex items-center gap-2.5 font-mono text-[0.68rem] tracking-[0.16em] whitespace-nowrap text-fog"><span class="h-3 w-[11px] shrink-0 bg-[#52525b] [clip-path:polygon(50%_0,93%_25%,93%_75%,50%_100%,7%_75%,7%_25%)]"></span>{section}&nbsp;&nbsp;/&nbsp;&nbsp;{name.toUpperCase()}</span>
  <span class="overflow-hidden font-mono text-[2.2rem] leading-none font-bold tracking-[0.02em] text-ellipsis whitespace-nowrap text-transparent [-webkit-text-stroke:1px_#26262b] select-none">{name.toUpperCase() || section}</span>
</div>

<style>
  /* ---- hero accent wash (layout via utilities in NS; accent must stay
     a runtime var, so this one rule remains CSS) ---- */
  :global(.doc-section .content .sec-hero) {
    background:
      radial-gradient(circle at 100% 0%, color-mix(in srgb, var(--sec-accent, #34d399) 16%, transparent), transparent 45%),
      radial-gradient(circle at 0% 100%, color-mix(in srgb, var(--sec-accent, #34d399) 9%, transparent), transparent 40%),
      #0b0b0d;
    border-color: color-mix(in srgb, var(--sec-accent, #34d399) 22%, #2a2a2e);
  }
  /* ---- hero typography (markdown-rendered h1/p can't take utilities) ---- */
  :global(.doc-section .content .sec-hero h1) {
    margin: 0;
    font-size: clamp(1.9rem, 4.5vw, 2.7rem);
  }
  :global(.doc-section .content .sec-hero > p) {
    max-width: 70ch;
    color: #d4d4d8;
  }
  :global(.doc-section .content .sec-hero > p:first-of-type) {
    font-size: 1.02rem;
    color: #e4e4e7;
  }

  /* ---- term lists (bold-lead items → numbered term cards) ---- */
  :global(.doc-section .content ul.sec-terms) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
    counter-reset: sec-term;
  }
  :global(.doc-section .content ul.sec-terms > li) {
    position: relative;
    counter-increment: sec-term;
    margin: 0;
    padding: 0.7rem 0.9rem 0.7rem 3.2rem;
    background: #0b0b0d;
    border: 1px solid #2a2a2e;
    border-left: 2px solid #3f3f46;
    border-radius: 0 10px 10px 0;
    transition: border-color 0.15s ease;
  }
  :global(.doc-section .content ul.sec-terms > li:hover) {
    border-color: #3f3f46;
    border-left-color: var(--sec-accent, #34d399);
  }
  :global(.doc-section .content ul.sec-terms > li::marker) {
    content: '';
  }
  :global(.doc-section .content ul.sec-terms > li::before) {
    content: counter(sec-term, decimal-leading-zero);
    position: absolute;
    left: 0.9rem;
    top: 0.85rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    color: #52525b;
  }
  :global(.doc-section .content ul.sec-terms > li > strong:first-child) {
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

  /* ---- pull-quote ---- */
  :global(.doc-section .content blockquote) {
    margin: 1.2rem 0 0;
    padding: 1.1rem 1.3rem;
    background: #0b0b0d;
    border: 1px solid #2a2a2e;
    border-left: 2px solid var(--sec-accent, #34d399);
    border-radius: 0 12px 12px 0;
  }
  :global(.doc-section .content blockquote p) {
    margin: 0;
    font-size: 1.02rem;
    line-height: 1.7;
    color: #e4e4e7;
  }

  /* ---- expansion panels (diagnostics) as cards ---- */
  :global(.doc-section .content .c-expansion) {
    margin: 0 0 0.5rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 12px;
    overflow: hidden;
    transition: border-color 0.15s ease;
  }
  :global(.doc-section .content .c-expansion:hover) {
    border-color: #3f3f46;
  }
  :global(.doc-section .content .c-expansion--expanded) {
    border-color: #2a2a2e;
  }
  :global(.doc-section .content .c-expansion--header) {
    padding: 0.8rem 1rem;
  }
  :global(.doc-section .content .c-expansion--title) {
    font-weight: 600;
    color: #e4e4e7;
  }
  :global(.doc-section .content .c-expansion--arrow) {
    transition: transform 0.2s ease;
  }
  :global(.doc-section .content .c-expansion--arrow-expanded) {
    transform: rotate(180deg);
  }
  :global(.doc-section .content .c-expansion--expanded .c-expansion--body) {
    border-top: 1px solid #1d1d20;
    padding: 0.9rem 1rem 1rem;
    color: #a1a1aa;
    font-size: 0.9rem;
    line-height: 1.7;
  }

  /* ---- content restyle, scoped to plugin pages ---- */
  :global(.doc-section .content) {
    counter-reset: sec-sec;
  }
  :global(.doc-section .content h1) {
    font-size: clamp(1.7rem, 4vw, 2.4rem);
    font-weight: 650;
    letter-spacing: -0.03em;
    line-height: 1.12;
    color: #fafafa;
    margin: 2.2rem 0 0.6rem;
  }
  :global(.doc-section .content h1 + p),
  :global(.doc-section .content h1 + p + p) {
    color: #d4d4d8;
    font-size: 0.95rem;
    line-height: 1.75;
    max-width: 68ch;
  }
  :global(.doc-section .content h2) {
    counter-increment: sec-sec;
    font-size: 1.2rem;
    font-weight: 650;
    letter-spacing: -0.02em;
    color: #f4f4f5;
    margin: 2.6rem 0 0.7rem;
    padding-top: 1.3rem;
    border-top: 1px solid #1d1d20;
  }
  :global(.doc-section .content h2::before) {
    content: counter(sec-sec, decimal-leading-zero);
    display: inline-block;
    margin-right: 0.7rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    font-weight: 400;
    letter-spacing: 0.1em;
    color: #52525b;
    vertical-align: 0.18em;
  }
  :global(.doc-section .content h3) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.74rem;
    font-weight: 600;
    letter-spacing: 0.14em;
    text-transform: uppercase;
    color: #8e8e93;
    margin: 2rem 0 0.6rem;
  }
  :global(.doc-section .content p),
  :global(.doc-section .content li) {
    color: #a1a1aa;
    font-size: 0.95rem;
    line-height: 1.75;
  }
  :global(.doc-section .content strong) {
    color: #f4f4f5;
    font-weight: 600;
  }
  :global(.doc-section .content a) {
    color: #f4f4f5;
    text-decoration: underline;
    text-decoration-color: #52525b;
    text-underline-offset: 3px;
  }
  :global(.doc-section .content a:hover) {
    text-decoration-color: #f4f4f5;
  }
  :global(.doc-section .content ul) {
    display: grid;
    gap: 0.45rem;
    padding-left: 1.1rem;
  }
  :global(.doc-section .content li::marker) {
    content: '–  ';
    color: #52525b;
  }

  /* ---- pipeline steps: numbered cards ---- */
  :global(.doc-section .content ol) {
    list-style: none;
    margin: 1rem 0 0;
    padding: 0;
    display: grid;
    gap: 0.5rem;
    counter-reset: sec-step;
  }
  :global(.doc-section .content ol > li) {
    position: relative;
    counter-increment: sec-step;
    display: block;
    margin: 0;
    padding: 0.65rem 0.9rem 0.65rem 3.2rem;
    background: #0c0c0e;
    border: 1px solid #1d1d20;
    border-radius: 10px;
  }
  :global(.doc-section .content ol > li::marker) {
    content: '';
  }
  :global(.doc-section .content ol > li::before) {
    content: counter(sec-step, decimal-leading-zero);
    position: absolute;
    left: 0.9rem;
    top: 0.85rem;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.72rem;
    color: #8e8e93;
  }

  /* ---- option/reference tables ---- */
  :global(.doc-section .content table) {
    width: 100%;
    border-collapse: separate;
    border-spacing: 0;
    margin: 1rem 0 0;
    font-size: 0.88rem;
    background: #0b0b0d;
    border: 1px solid #2a2a2e;
    border-radius: 12px;
    overflow: hidden;
  }
  :global(.doc-section .content thead th) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.68rem;
    font-weight: 600;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: #8e8e93;
    text-align: left;
    padding: 0.7rem 0.9rem;
    background: rgb(255 255 255 / 0.03);
    border-bottom: 1px solid #2a2a2e;
  }
  :global(.doc-section .content tbody td) {
    text-align: left;
    vertical-align: top;
    padding: 0.65rem 0.9rem;
    border-bottom: 1px solid #1d1d20;
    color: #a1a1aa;
    line-height: 1.6;
  }
  :global(.doc-section .content tbody tr:last-child td) {
    border-bottom: 0;
  }
  :global(.doc-section .content tbody tr) {
    transition: background-color 0.15s ease;
  }
  :global(.doc-section .content tbody tr:hover) {
    background: rgb(255 255 255 / 0.025);
  }
  :global(.doc-section .content tbody td:first-child) {
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.8rem;
    color: #e4e4e7;
    white-space: nowrap;
  }
  :global(.doc-section .content td code),
  :global(.doc-section .content li code),
  :global(.doc-section .content p code) {
    color: #e4e4e7;
  }
</style>
