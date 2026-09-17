<script lang="ts">
  import { response } from "../../lib/stores";
  import { formatJsonLines, type CodeLine } from "../../lib/format/jsonHighlight";
  import { rawCodeLines, type RawFormat } from "../../lib/format/raw";
  import { parseMetadata } from "../../lib/metadata/metadata";
  import { useClipboard } from "../../lib/clipboard/useClipboard.svelte";

  let {
    onSwitchToRequest,
    onInvoke,
    isInvoking = false,
  }: {
    onSwitchToRequest?: () => void;
    onInvoke?: () => void;
    isInvoking?: boolean;
  } = $props();

  let rawFormat = $state<RawFormat>("base64");

  const { copiedSection, copy } = useClipboard();

  const messageLines = $derived.by<CodeLine[]>(() => {
    if ($response === null) return [];
    return formatJsonLines($response.body);
  });

  const rawLines = $derived.by<CodeLine[]>(() => {
    if ($response === null) return [];
    return rawCodeLines($response.body, $response.rawBase64, rawFormat);
  });

  const trailersList = $derived.by(() => {
    if ($response === null) return [];
    return parseMetadata(undefined, $response.trailers, {
      status: $response.status,
      statusName: $response.statusName,
      statusCode: $response.statusCode,
      contentType: $response.contentType,
    });
  });

  function getMessageRaw(): string {
    if ($response === null) return "";
    return JSON.stringify($response.body, null, 2);
  }

  function getRawResponseText(): string {
    return rawLines.map((l) => l.raw).join("\n");
  }
</script>

{#if $response === null}
  <div class="flex h-full min-h-[360px] flex-col items-center justify-center gap-4 p-8 text-center">
    <div class="flex size-16 items-center justify-center rounded-2xl bg-panel2 border border-border/80 text-muted shadow-sm">
      <svg width="32" height="32" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
        <circle cx="8" cy="8" r="6" />
        <path d="M8 4.5v3.5l2.5 1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </div>

    <div class="max-w-md">
      <h3 class="m-0 text-[15px] font-bold text-text">No response yet</h3>
      <p class="mt-1.5 mb-0 text-[13px] leading-relaxed text-muted">
        Run an invocation to execute this RPC method and inspect the server response here.
      </p>
    </div>

    {#if onInvoke}
      <div class="mt-2 flex items-center gap-2.5">
        <button
          type="button"
          class="flex cursor-pointer items-center gap-2 rounded-xl bg-accent px-4 py-2 text-[13px] font-semibold text-white shadow-sm transition-all hover:brightness-110 active:scale-95 disabled:opacity-50"
          onclick={onInvoke}
          disabled={isInvoking}
        >
          {#if isInvoking}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0 animate-spin">
              <circle cx="8" cy="8" r="6" opacity="0.3" />
              <path d="M14 8a6 6 0 0 0-6-6" stroke-linecap="round" />
            </svg>
          {:else}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="currentColor">
              <path d="M4 2.5v11l9-5.5-9-5.5z" />
            </svg>
          {/if}
          <span>{isInvoking ? "Invoking…" : "Invoke Method"}</span>
        </button>

        {#if onSwitchToRequest}
          <button
            type="button"
            class="cursor-pointer rounded-xl border border-border bg-panel2 px-3.5 py-2 text-[13px] font-medium text-muted transition-colors hover:bg-panel hover:text-text"
            onclick={onSwitchToRequest}
          >
            Edit Request
          </button>
        {/if}
      </div>
    {/if}
  </div>
{:else}
  {@const result = $response}
  <div class="flex h-full min-h-0 flex-col gap-4 overflow-y-auto p-5">
    <!-- Top Status Bar -->
    <div class="flex flex-wrap items-center justify-between gap-3 border-b border-border/50 pb-3.5">
      <div class="flex items-center gap-2.5">
        {#if result.status === "OK"}
          <span class="flex size-6 shrink-0 items-center justify-center rounded-full bg-green/15 text-green ring-1 ring-green/30">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </span>
        {:else}
          <span class="flex size-6 shrink-0 items-center justify-center rounded-full bg-red/15 text-red ring-1 ring-red/30">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M4.5 4.5 11.5 11.5M11.5 4.5 4.5 11.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </span>
        {/if}

        <span class="text-[15px] font-bold text-text">Response</span>

        <span
          class="rounded-md px-2 py-0.5 font-mono text-[11px] font-bold uppercase tracking-wider {result.status === 'OK' ? 'bg-green/15 text-green ring-1 ring-green/30' : 'bg-red/15 text-red ring-1 ring-red/30'}"
        >
          {result.status === "OK" ? (result.statusName ?? "OK") : (result.statusName ?? "ERROR")}
        </span>
      </div>

      <div class="flex items-center gap-4 text-[12px] text-muted">
        <div class="flex items-center gap-1.5 font-mono">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="8" cy="8" r="6" />
            <path d="M8 5v3l2 1" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>{result.durationMs} ms</span>
        </div>

        <div class="flex items-center gap-1.5 font-mono">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="8" cy="8" r="6" />
            <path d="M2 8h12M8 2a9 9 0 0 1 0 12M8 2a9 9 0 0 0 0 12" stroke-linecap="round" />
          </svg>
          <span>{result.contentType}</span>
        </div>
      </div>
    </div>

    <!-- Section 1: Message -->
    <div>
      <h4 class="mt-0 mb-2 text-[13px] font-semibold text-text">Message</h4>
      <div class="relative overflow-hidden rounded-xl border border-border/80 bg-dark-card">
        <button
          type="button"
          class="absolute top-2.5 right-2.5 z-10 flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel2/90 px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel hover:text-text"
          onclick={() => copy(getMessageRaw(), "message")}
        >
          {#if copiedSection === "message"}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="2">
              <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <span class="text-green">Copied</span>
          {:else}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="5" y="5" width="8" height="8" rx="1.5" />
              <path d="M3 11V3a1 1 0 0 1 1-1h8" stroke-linecap="round" />
            </svg>
            <span>Copy</span>
          {/if}
        </button>

        <div class="flex overflow-x-auto p-4 font-mono text-[12.5px] leading-[22px]">
          <div class="shrink-0 select-none border-r border-border/30 pr-4 text-right text-muted/40">
            {#each messageLines as line}
              <div>{line.number}</div>
            {/each}
          </div>
          <div class="min-w-0 flex-1 pl-4">
            {#each messageLines as line}
              <div class="whitespace-pre">{@html line.html || "&nbsp;"}</div>
            {/each}
          </div>
        </div>
      </div>
    </div>

    <!-- Section 2: Raw response -->
    <div>
      <div class="mb-2 flex items-center gap-2">
        <h4 class="m-0 text-[13px] font-semibold text-text">Raw response</h4>
        <div class="relative inline-block">
          <select
            bind:value={rawFormat}
            class="appearance-none cursor-pointer rounded-md border border-border bg-panel2 px-2.5 py-0.5 pr-6 font-sans text-[11px] text-muted hover:text-text focus:outline-none"
          >
            <option value="base64">Protobuf (base64)</option>
            <option value="hex">Hex</option>
            <option value="raw">JSON (raw)</option>
          </select>
          <svg
            class="pointer-events-none absolute right-1.5 top-1/2 -translate-y-1/2 text-muted"
            width="12"
            height="12"
            viewBox="0 0 16 16"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
          >
            <path d="m4 6 4 4 4-4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </div>
      </div>

      <div class="relative overflow-hidden rounded-xl border border-border/80 bg-dark-card">
        <button
          type="button"
          class="absolute top-2.5 right-2.5 z-10 flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel2/90 px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel hover:text-text"
          onclick={() => copy(getRawResponseText(), "raw")}
        >
          {#if copiedSection === "raw"}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="2">
              <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <span class="text-green">Copied</span>
          {:else}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="5" y="5" width="8" height="8" rx="1.5" />
              <path d="M3 11V3a1 1 0 0 1 1-1h8" stroke-linecap="round" />
            </svg>
            <span>Copy</span>
          {/if}
        </button>

        <div class="flex overflow-x-auto p-4 font-mono text-[12.5px] leading-[22px]">
          <div class="shrink-0 select-none border-r border-border/30 pr-4 text-right text-muted/40">
            {#each rawLines as line}
              <div>{line.number}</div>
            {/each}
          </div>
          <div class="min-w-0 flex-1 pl-4 text-text/90">
            {#each rawLines as line}
              <div class="whitespace-pre">{line.raw || " "}</div>
            {/each}
          </div>
        </div>
      </div>
    </div>

    <!-- Section 3: Response trailers -->
    <div>
      <h4 class="mt-0 mb-2 text-[13px] font-semibold text-text">Response trailers</h4>
      <div class="overflow-hidden rounded-xl border border-border/80 bg-dark-card">
        <table class="w-full border-collapse">
          <tbody>
            {#each trailersList as [key, value], i}
              <tr class={i < trailersList.length - 1 ? "border-b border-border/40" : ""}>
                <td class="w-[200px] px-4 py-2.5 font-mono text-[12px] text-muted">
                  {key}
                </td>
                <td class="px-4 py-2.5 font-mono text-[12px] text-text">
                  {value === "" ? '""' : value}
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  </div>
{/if}
