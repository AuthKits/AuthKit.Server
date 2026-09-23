<script>
  import { page } from '$app/state'
  import { resolveLocale } from 'virtual:sveltepress/locale'
  import { getLocale, setLocale } from '$lib/paraglide/runtime'
  // side effect: registers the "custom-sveltepress" client strategy
  import '$lib/locale-strategy'
  // brand overrides (monochrome instead of theme rose)
  import '../theme.css'

  let { children } = $props()

  // Keep the Paraglide runtime locale in sync with the SveltePress locale
  // (derived from the URL prefix) on client-side navigations.
  // Also hides the code-block language badge when the fence has no language
  // (the theme renders a literal "null" there).
  $effect(() => {
    const locale = resolveLocale(page.url.pathname)
    const tag = locale?.lang === 'pl' ? 'pl' : 'en'
    if (getLocale() !== tag) setLocale(tag, { reload: false })
    for (const el of document.querySelectorAll('.svp-code-block--lang')) {
      if (el.textContent?.trim() === 'null') el.setAttribute('hidden', '')
    }
  })
</script>

{@render children()}
