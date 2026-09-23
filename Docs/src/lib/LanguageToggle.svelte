<script>
  import { page } from '$app/state'
  import { resolveLocaleSwitch } from 'virtual:sveltepress/locale'
  import * as m from '$lib/paraglide/messages.js'
  import { setLocale } from '$lib/paraglide/runtime'
  import { localeFromPathname } from '$lib/locale-strategy'
  import { defaultLocale, siteLocales } from '$lib/site-locales'
  import '$lib/locale-strategy'

  const langs = siteLocales.map(l => ({ code: l.code, label: l.label, short: l.short, base: l.prefix }))

  // Derived from the URL (reactive) so client side navigation updates it.
  const current = $derived(localeFromPathname(page.url.pathname))
  const currentLabel = $derived(langs.find((l) => l.code === current)?.short ?? defaultLocale.short)

  let open = $state(false)
  let wrap = $state(null)

  function targetFor(code) {
    const lang = langs.find((l) => l.code === code)
    return lang ? resolveLocaleSwitch(page.url.pathname, lang.base) : null
  }

  function choose(code) {
    if (code === current) {
      open = false
      return
    }
    // Persist for the cookie strategy, then navigate to the same logical
    // page in the other locale (SveltePress owns the URL routing).
    setLocale(code, { reload: false })
    const t = targetFor(code)
    open = false
    if (t) window.location.href = t.href
  }

  // Close on Escape / outside click.
  $effect(() => {
    if (!open) return
    const onKey = (e) => {
      if (e.key === 'Escape') open = false
    }
    const onPointer = (e) => {
      if (wrap && !wrap.contains(e.target)) open = false
    }
    window.addEventListener('keydown', onKey)
    window.addEventListener('pointerdown', onPointer)
    return () => {
      window.removeEventListener('keydown', onKey)
      window.removeEventListener('pointerdown', onPointer)
    }
  })
</script>

<div class="relative" bind:this={wrap}>
  <button
    class="inline-flex cursor-pointer items-center gap-1.5 rounded-full border border-edge2 bg-transparent px-2.5 py-1.5 font-mono text-[0.76rem] tracking-[0.06em] text-fog transition-colors hover:border-fog hover:text-paper"
    onclick={() => (open = !open)}
    aria-haspopup="listbox"
    aria-expanded={open}
    title={m['locale.language']()}
    aria-label={m['locale.language']()}
  >
    <span>{currentLabel}</span>
    <svg
      class="size-3 transition-transform duration-200 {open ? 'rotate-180' : ''}"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      aria-hidden="true"
    >
      <path d="M6 9l6 6 6-6" stroke-linecap="round" stroke-linejoin="round" />
    </svg>
  </button>
  {#if open}
    <ul class="absolute top-[calc(100%+8px)] right-0 z-50 m-0 min-w-44 list-none rounded-xl border border-edge2 bg-card p-1 shadow-[0_12px_32px_rgba(0,0,0,0.5)]" role="listbox" aria-label={m['locale.language']()}>
      {#each langs as l}
        <li>
          <button
            role="option"
            aria-selected={l.code === current}
            onclick={() => choose(l.code)}
            class="flex w-full cursor-pointer items-center gap-2.5 rounded-lg border-0 bg-transparent px-2.5 py-2 text-left font-mono text-[0.8rem] tracking-[0.04em] text-fog hover:bg-well hover:text-paper aria-selected:text-paper"
          >
            <span class="min-w-[1.6rem] text-[0.7rem] text-faint">{l.short}</span>
            <span>{l.label}</span>
            {#if l.code === current}<span class="ml-auto text-faint" aria-hidden="true">✓</span>{/if}
          </button>
        </li>
      {/each}
    </ul>
  {/if}
</div>
