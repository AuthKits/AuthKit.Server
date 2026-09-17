<script lang="ts">
  import type { GrpcMethod, GrpcService } from "../../lib/types";

  let { service, method }: { service: GrpcService; method: GrpcMethod } = $props();
</script>

<div class="flex flex-wrap items-start justify-between gap-4">
  <!-- Left info -->
  <div class="min-w-0 flex-1">
    <div class="flex flex-wrap items-center gap-2.5">
      <span class="text-accent2">
        <svg width="20" height="20" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.75">
          <path d="M8 1.5l5.5 3.17v6.66L8 14.5l-5.5-3.17V4.67L8 1.5z" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </span>
      <h2 class="m-0 truncate text-[19px] font-bold text-text">{method.name}</h2>
      <span
        class="inline-flex shrink-0 items-center rounded-md bg-accent px-2 py-0.5 font-mono text-[10px] font-bold uppercase tracking-wider text-white"
      >
        RPC
      </span>
    </div>

    <div class="mt-2 flex items-center gap-1.5 font-mono text-[12px] text-muted">
      <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="2">
        <rect x="2" y="2" width="12" height="12" rx="3" />
      </svg>
      <span class="truncate">
        {service.package}.{service.name}/{method.name}
      </span>
    </div>

    {#if method.description}
      <p class="mb-0 mt-2 text-[13px] leading-relaxed text-muted">
        {method.description}
      </p>
    {/if}
  </div>

  <!-- Right compact type cards -->
  <div class="flex shrink-0 flex-col gap-2 min-w-[240px] max-w-[320px]">
    <!-- Request card -->
    <div class="flex items-center gap-2.5 rounded-xl border border-border/80 bg-panel2/70 p-2.5 px-3">
      <span class="flex size-7 shrink-0 items-center justify-center rounded-lg bg-panel border border-border text-muted">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="2" y="2" width="12" height="12" rx="3" />
          <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
        </svg>
      </span>
      <div class="min-w-0 flex-1">
        <div class="text-[10.5px] font-medium text-muted">Request</div>
        <div class="truncate font-mono text-[11px] text-text">
          {method.fullRequestPath}
        </div>
      </div>
    </div>

    <!-- Response card -->
    <div class="flex items-center gap-2.5 rounded-xl border border-border/80 bg-panel2/70 p-2.5 px-3">
      <span class="flex size-7 shrink-0 items-center justify-center rounded-lg bg-panel border border-border text-muted">
        <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="2" y="2" width="12" height="12" rx="3" />
          <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
        </svg>
      </span>
      <div class="min-w-0 flex-1">
        <div class="text-[10.5px] font-medium text-muted">Response</div>
        <div class="truncate font-mono text-[11px] text-text">
          {method.fullResponsePath}
        </div>
      </div>
    </div>
  </div>
</div>
