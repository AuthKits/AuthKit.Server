<script lang="ts">
  let {
    isInvoking,
    onInvoke,
    onFillDefaults,
    onFormatJson,
    onClear,
  }: {
    isInvoking: boolean;
    onInvoke: () => void;
    onFillDefaults: () => void;
    onFormatJson: () => void;
    onClear: () => void;
  } = $props();
</script>

<div
  class="sticky bottom-0 z-10 flex shrink-0 items-center justify-between gap-2 rounded-lg border border-border bg-panel px-4 py-2.5"
>
  <div class="flex flex-wrap items-center gap-2">
    <button
      type="button"
      class="inline-flex cursor-pointer items-center gap-2 rounded-lg border border-accent bg-accent px-3.5 py-1.5 text-[13px] font-semibold text-white transition-all duration-150 hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-2"
      disabled={isInvoking}
      onclick={onInvoke}
    >
      {#if isInvoking}
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0 animate-spin">
          <circle cx="8" cy="8" r="6" opacity="0.3" />
          <path d="M14 8a6 6 0 0 0-6-6" stroke-linecap="round" />
        </svg>
      {:else}
        <svg width="11" height="11" viewBox="0 0 16 16" fill="currentColor" class="shrink-0">
          <path d="M4 2.5v11l9-5.5-9-5.5Z" />
        </svg>
      {/if}
      {isInvoking ? "Invoking…" : "Invoke"}
    </button>
    <button
      type="button"
      class="cursor-pointer rounded-lg border border-border bg-panel2 px-3.5 py-1.5 text-[13px] text-text transition-all duration-150 hover:brightness-110 focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-2"
      onclick={onFillDefaults}
    >
      Fill defaults
    </button>
    <button
      type="button"
      class="cursor-pointer rounded-lg border border-border bg-panel2 px-3.5 py-1.5 text-[13px] text-text transition-all duration-150 hover:brightness-110 focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-2"
      onclick={onFormatJson}
    >
      &lt;/&gt; Format JSON
    </button>
  </div>
  <button
    type="button"
    class="cursor-pointer rounded-lg border border-border bg-panel2 px-3 py-1.5 text-[13px] text-muted transition-all duration-150 hover:brightness-110 hover:text-text focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent focus-visible:outline-offset-2"
    onclick={onClear}
  >
    Clear
  </button>
</div>
