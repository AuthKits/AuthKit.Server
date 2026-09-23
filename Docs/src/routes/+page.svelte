<script module>
  export const frontmatter = {
    title: 'AuthKit',
    home: false,
    layout: false,
    header: false,
  }
  export const prerender = true
</script>

<script>
  import { goto } from '$app/navigation'
  import { onMount } from 'svelte'
  import { defaultLocale, siteLocales } from '$lib/site-locales'

  const home = defaultLocale.prefix

  onMount(() => {
    goto(home, { replaceState: true })
  })
</script>

<svelte:head>
  <title>AuthKit</title>
  <link rel="canonical" href={home} />
  <meta http-equiv="refresh" content={`0; url=${home}`} />
</svelte:head>

<main>
  <p>
    {#each siteLocales as l}
      <a href={l.prefix}>{l.label}</a>{' '}
    {/each}
  </p>
</main>

<style>
  main {
    display: grid;
    place-items: center;
    min-height: 100svh;
    background: #060607;
    color: #8e8e93;
    font-family: ui-monospace, 'SF Mono', Menlo, monospace;
    font-size: 0.85rem;
  }
  a {
    color: #f4f4f5;
  }
</style>
