<script>
  import LanguageToggle from '$lib/LanguageToggle.svelte'
  import { motion } from '@humanspeak/svelte-motion'
  import * as m from '$lib/paraglide/messages.js'
  import { defaultLocale, siteLocales } from '$lib/site-locales'
  import { buildFeatures, buildHexes, buildThirdNav } from './chrome/landing-data'

  let { base = `/${defaultLocale.code}` } = $props()
  const localeEntry = siteLocales.find(l => `/${l.code}` === base) ?? defaultLocale
  const locale = localeEntry.code
  const t = (key) => m[key](undefined, { locale })

  const hexes = $derived(buildHexes(t, base))
  const features = $derived(buildFeatures(t))

  // Third nav item per locale registry (default: ADRs).
  const thirdNav = $derived(buildThirdNav(t, base, localeEntry.navThird))
  const otherLang = $derived.by(() => {
    const other = siteLocales.find(l => l.code !== locale) ?? defaultLocale
    return { label: other.label, href: other.prefix }
  })

  let open = $state(null)

  function close() {
    open = null
  }

  // Escape closes the dialog. (A <svelte:window> tag can't be used here:
  // SveltePress wraps the whole page in <PageLayout>, and svelte:window
  // must not sit inside an element.)
  $effect(() => {
    const onKey = (e) => {
      if (e.key === 'Escape') close()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  })
</script>

<svelte:head>
  <title>{t('landing.metaTitle')}</title>
  <meta name="description" content={t('landing.metaDescription')} />
  <meta name="theme-color" content="#060607" />
</svelte:head>

<div class="page relative m-0 min-h-svh overflow-clip bg-night font-[Inter,-apple-system,'Segoe_UI',sans-serif] text-paper [-webkit-font-smoothing:antialiased]">
  <div class="pointer-events-none absolute inset-0 overflow-hidden" aria-hidden="true">
    <div class="absolute top-[-320px] left-1/2 h-[560px] w-[min(900px,120vw)] -translate-x-1/2 bg-[radial-gradient(closest-side,rgba(255,255,255,0.07),transparent)]"></div>
    <div class="absolute top-[120px] left-1/2 -translate-x-1/2 text-[clamp(6rem,18vw,17rem)] leading-none font-extrabold tracking-[-0.04em] whitespace-nowrap text-transparent [-webkit-text-stroke:1px_#17171a] select-none">AUTHKIT</div>
    <div class="absolute top-[32%] left-[-160px] size-[420px] rounded-full bg-white/[0.025] blur-[90px]"></div>
    <div class="absolute top-[58%] right-[-140px] size-[380px] rounded-full bg-white/[0.02] blur-[90px]"></div>
  </div>
  <nav class="relative mx-auto flex max-w-[68rem] items-center justify-between px-6 py-5">
    <a class="text-[1rem] font-semibold tracking-[-0.02em] text-paper no-underline" href="{base}/">AuthKit</a>
    <div class="flex items-center gap-6 text-[0.86rem]">
      <a class="text-fog no-underline hover:text-paper max-md:hidden" href="{base}/guide/introduction/">{t('landing.navDocs')}</a>
      <a class="text-fog no-underline hover:text-paper max-md:hidden" href="{base}/reference/rest-api/">{t('landing.navApi')}</a>
      <a class="text-fog no-underline hover:text-paper max-md:hidden" href={thirdNav.href}>{thirdNav.label}</a>
      <a class="text-fog no-underline hover:text-paper max-md:hidden" href="https://github.com/AuthKits/AuthKit.Server">{t('landing.navGithub')}</a>
      <LanguageToggle />
    </div>
  </nav>

  <motion.header
    class="relative mx-auto max-w-[68rem] px-6 pt-28 pb-16 text-center max-md:pt-16"
    initial={{ opacity: 0, y: 24 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.6, ease: 'easeOut' }}
  >
    <h1 class="m-0 text-[clamp(2.8rem,7vw,5rem)] leading-[1.02] font-semibold tracking-[-0.045em]">{t('landing.heroTitleA')}<br />{t('landing.heroTitleB')}</h1>
    <p class="mx-auto mt-6 max-w-[36rem] text-[1.06rem] leading-[1.65] text-fog">{t('landing.heroSub')}</p>
    <div class="mt-9 flex justify-center gap-2.5">
      <a class="rounded-full bg-paper px-6 py-2.5 text-[0.9rem] font-medium text-night no-underline hover:bg-white" href="{base}/guide/introduction/">{t('landing.heroPrimary')}</a>
      <a class="rounded-full px-6 py-2.5 text-[0.9rem] font-medium text-paper no-underline hover:text-fog" href="{base}/guide/quick-start/">{t('landing.heroSecondary')}</a>
    </div>
  </motion.header>

  <section class="relative mx-auto flex max-w-[68rem] flex-wrap items-center justify-center gap-1.5 px-6 pt-16 max-md:flex-col max-md:gap-0" aria-label={t('landing.flowLabel')}>
    {#each hexes as h, i}
      <motion.button
        class="group grid aspect-[1/1.12] w-[clamp(210px,26vw,270px)] shrink-0 cursor-pointer place-items-center border-0 bg-[#26262b] p-0 font-inherit text-inherit transition-colors [clip-path:polygon(50%_0,93%_25%,93%_75%,50%_100%,7%_75%,7%_25%)] hover:bg-paper"
        onclick={() => (open = i)}
        aria-haspopup="dialog"
        aria-label="{h.title} — {t('landing.detailsMore')}"
        initial={{ opacity: 0, y: 16 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: '-40px' }}
        transition={{ duration: 0.45, delay: i * 0.08 }}
        whileHover={{ scale: 1.03 }}
        whileTap={{ scale: 0.98 }}
      >
        <span class="flex h-[96%] w-[96%] flex-col items-center justify-center gap-[0.45rem] bg-card px-9 text-center [clip-path:polygon(50%_0,93%_25%,93%_75%,50%_100%,7%_75%,7%_25%)]">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.25" aria-hidden="true" class="mb-0.5 size-10 -translate-y-[1.1rem] text-paper"><path d={h.icon} stroke-linecap="round" stroke-linejoin="round" /></svg>
          <span class="font-mono text-[0.6rem] tracking-[0.14em] text-faint">{h.num}</span>
          <strong class="font-mono text-[0.7rem] leading-[1.5] font-semibold wrap-anywhere">{h.title}</strong>
          <span class="font-mono text-[0.64rem] leading-[1.5] text-fog wrap-anywhere">{h.sub}</span>
          <span class="border-b border-edge2 pb-px font-mono text-[0.64rem] tracking-[0.1em] text-faint group-hover:border-paper group-hover:text-paper">{t('landing.detailsMore')}</span>
        </span>
      </motion.button>
      {#if i < hexes.length - 1}
        <span class="text-[1.2rem] text-faint select-none max-md:-my-1.5 max-md:rotate-90" aria-hidden="true">→</span>
      {/if}
    {/each}
  </section>

  {#if open !== null}
    {@const h = hexes[open]}
    <div
      class="fixed inset-0 z-[100] grid place-items-center bg-black/70 p-6 backdrop-blur-sm"
      role="presentation"
      onclick={(e) => {
        if (e.target === e.currentTarget) close()
      }}
    >
      <div class="w-full max-w-[34rem] rounded-[14px] border border-edge2 bg-card p-6" role="dialog" aria-modal="true" aria-label={h.title}>
        <div class="flex items-center justify-between">
          <span class="font-mono text-[0.6rem] tracking-[0.14em] text-faint">{h.num}</span>
          <button class="size-[1.9rem] cursor-pointer rounded-full border border-edge2 bg-transparent text-[0.8rem] leading-none text-fog hover:border-fog hover:text-paper" onclick={close} aria-label={t('landing.dialogClose')}>✕</button>
        </div>
        <h3 class="mt-3 mb-2 font-mono text-base font-semibold tracking-[-0.02em]">{h.title}</h3>
        <p class="m-0 text-[0.9rem] leading-[1.7] text-mist">{h.body}</p>
        <pre class="mt-4 overflow-x-auto rounded-lg border border-edge bg-night px-4 py-3.5"><code class="font-mono text-[0.76rem] leading-[1.7] whitespace-pre-wrap text-[#c9c9ce]">{h.code}</code></pre>
        <a class="mt-4 inline-block border-b border-edge2 pb-0.5 font-mono text-[0.8rem] text-paper no-underline hover:border-paper" href={h.link}>{h.linkLabel}</a>
      </div>
    </div>
  {/if}

  <section class="relative mx-auto grid max-w-[68rem] grid-cols-3 items-start gap-5 px-6 pt-20 pb-12 max-md:grid-cols-1 max-md:gap-8">
    {#each features as f, i}
      <motion.div
        class="group rounded-[14px] border border-edge bg-card p-6 hover:border-edge2"
        initial={{ opacity: 0, y: 20 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, margin: '-40px' }}
        transition={{ duration: 0.5, delay: i * 0.1 }}
        whileHover={{ y: -4 }}
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true" class="block size-[30px] text-fog transition group-hover:-translate-y-0.5 group-hover:text-paper"><path d={f.icon} stroke-linecap="round" stroke-linejoin="round" /></svg>
        <h2 class="mt-4 mb-2 text-[0.95rem] font-semibold tracking-[-0.01em] text-paper">{f.t}</h2>
        <p class="m-0 text-[0.88rem] leading-[1.65] text-fog">{f.d}</p>
      </motion.div>
    {/each}
  </section>

  <footer class="mx-auto mt-12 flex max-w-[68rem] justify-between border-t border-edge p-6 text-[0.8rem] text-faint">
    <span>{t('landing.mit')}</span>
    <a href={otherLang.href} class="text-fog no-underline">{otherLang.label}</a>
  </footer>
</div>

<style>
  :global(body:has(.page) header.header) {
    display: none;
  }
  :global(html:has(.page), body:has(.page)) {
    background: #060607;
    color-scheme: dark;
  }
  :global(html:has(.page)) {
    scrollbar-color: #2a2a2e #060607;
  }
  :global(html:has(.page)::-webkit-scrollbar) {
    width: 10px;
  }
  :global(html:has(.page)::-webkit-scrollbar-track) {
    background: #060607;
  }
  :global(html:has(.page)::-webkit-scrollbar-thumb) {
    background: #2a2a2e;
    border-radius: 8px;
    border: 2px solid #060607;
  }
  :global(html:has(.page)::-webkit-scrollbar-thumb:hover) {
    background: #3f3f46;
  }
</style>
