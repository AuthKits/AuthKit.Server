<script>
  import { getIcon } from './chrome/icons'

  let { cards = [], columns = 3, linkLabel = 'Read more →' } = $props()
</script>

<div class="mt-6 grid gap-x-6 gap-y-2.5 {columns === 2 ? 'md:grid-cols-2' : 'md:grid-cols-3'}">
  {#each cards as card}
    <div class="group grid grid-cols-[2rem_1fr] items-start gap-x-4 rounded-[14px] px-1.5 py-3.5 text-[0.88rem] leading-[1.65] transition-colors hover:bg-black/[0.03] dark:hover:bg-white/[0.03] [&>*]:col-start-2 [&>*]:min-w-0">
      <span class="col-start-1 row-[1/4] mt-0.5 size-8 self-start text-mist transition-colors group-hover:text-[#18181b] dark:text-[#d4d4d8] dark:group-hover:text-paper" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" class="size-full">
          {@html getIcon(card.icon)}
        </svg>
      </span>
      <strong class="text-[0.98rem] tracking-[-0.01em] text-[#18181b] dark:text-paper">{card.title}</strong>
      <span class="text-[#52525b] dark:text-mist">{card.desc}</span>
      {#if card.href}
        <a href={card.href} class="mt-auto pt-2 text-[0.82rem] font-semibold text-[#dc2626] no-underline transition-colors hover:underline hover:underline-offset-[3px] dark:text-[#f87171]">{card.linkLabel ?? linkLabel}</a>
      {/if}
    </div>
  {/each}
</div>
