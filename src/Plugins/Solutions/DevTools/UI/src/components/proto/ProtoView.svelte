<script lang="ts">
  import { requestJson, response } from "../../lib/stores";
  import type { GrpcMethod, GrpcService } from "../../lib/types";
  import {
    formatJsonLines,
    formatProtoStruct,
    highlightProto,
    type CodeLine,
  } from "../../lib/format/jsonHighlight";
  import {
    parseJsonSafe,
    requestProtoCode,
    responseProtoCode,
  } from "../../lib/proto/protoView";
  import { useClipboard } from "../../lib/clipboard/useClipboard.svelte";

  let { service, method }: { service: GrpcService; method: GrpcMethod } = $props();

  const { copiedSection, copy } = useClipboard();

  const requestProto = $derived(requestProtoCode(service, method));
  const responseProto = $derived(responseProtoCode(service, method));
  const requestJsonData = $derived(parseJsonSafe($requestJson));

  const responseJsonData = $derived(() => {
    if ($response?.body) return $response.body;
    return {};
  });

  const requestJsonLines = $derived.by<CodeLine[]>(() => {
    return formatJsonLines(requestJsonData);
  });

  const responseJsonLines = $derived.by<CodeLine[]>(() => {
    return formatJsonLines(responseJsonData);
  });
</script>

<div class="flex flex-col gap-4 overflow-y-auto p-2">
  <!-- Row 1: Request Proto & Response Proto side-by-side -->
  <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
    <!-- Request Proto Card -->
    <div class="rounded-xl border border-border/80 bg-dark-card overflow-hidden">
      <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-2.5">
        <div class="flex items-center gap-2 font-mono text-[12px] font-semibold text-text">
          <span class="text-accent2">&lt;/&gt;</span>
          <span>Request Proto</span>
        </div>

        <button
          type="button"
          class="flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={() => copy(requestProto, "req-proto")}
        >
          {#if copiedSection === "req-proto"}
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
      </div>

      <div class="p-4 font-mono text-[12px] leading-relaxed overflow-x-auto text-text">
        <pre class="m-0 whitespace-pre">{@html highlightProto(requestProto)}</pre>
      </div>
    </div>

    <!-- Response Proto Card -->
    <div class="rounded-xl border border-border/80 bg-dark-card overflow-hidden">
      <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-2.5">
        <div class="flex items-center gap-2 font-mono text-[12px] font-semibold text-text">
          <span class="text-accent2">&lt;/&gt;</span>
          <span>Response Proto</span>
        </div>

        <button
          type="button"
          class="flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={() => copy(responseProto, "res-proto")}
        >
          {#if copiedSection === "res-proto"}
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
      </div>

      <div class="p-4 font-mono text-[12px] leading-relaxed overflow-x-auto text-text">
        <pre class="m-0 whitespace-pre">{@html highlightProto(responseProto)}</pre>
      </div>
    </div>
  </div>

  <!-- Row 2: Request (JSON) & Response (JSON) side-by-side with line numbers -->
  <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
    <!-- Request (JSON) -->
    <div class="rounded-xl border border-border/80 bg-dark-card overflow-hidden">
      <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-2.5">
        <div class="flex items-center gap-2 font-mono text-[12px] font-semibold text-text">
          <span class="text-accent2">{`{ }`}</span>
          <span>Request <span class="text-muted font-normal">(JSON)</span></span>
        </div>

        <button
          type="button"
          class="flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={() => copy(JSON.stringify(requestJsonData, null, 2), "req-json")}
        >
          {#if copiedSection === "req-json"}
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
      </div>

      <div class="flex overflow-x-auto p-4 font-mono text-[12.5px] leading-[22px]">
        <div class="shrink-0 select-none border-r border-border/30 pr-4 text-right text-muted/40">
          {#each requestJsonLines as line}
            <div>{line.number}</div>
          {/each}
        </div>
        <div class="min-w-0 flex-1 pl-4">
          {#each requestJsonLines as line}
            <div class="whitespace-pre">{@html line.html || "&nbsp;"}</div>
          {/each}
        </div>
      </div>
    </div>

    <!-- Response (JSON) -->
    <div class="rounded-xl border border-border/80 bg-dark-card overflow-hidden">
      <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-2.5">
        <div class="flex items-center gap-2 font-mono text-[12px] font-semibold text-text">
          <span class="text-accent2">{`{ }`}</span>
          <span>Response <span class="text-muted font-normal">(JSON)</span></span>
        </div>

        <button
          type="button"
          class="flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={() => copy(JSON.stringify(responseJsonData, null, 2), "res-json")}
        >
          {#if copiedSection === "res-json"}
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
      </div>

      <div class="flex overflow-x-auto p-4 font-mono text-[12.5px] leading-[22px]">
        <div class="shrink-0 select-none border-r border-border/30 pr-4 text-right text-muted/40">
          {#each responseJsonLines as line}
            <div>{line.number}</div>
          {/each}
        </div>
        <div class="min-w-0 flex-1 pl-4">
          {#each responseJsonLines as line}
            <div class="whitespace-pre">{@html line.html || "&nbsp;"}</div>
          {/each}
        </div>
      </div>
    </div>
  </div>

  <!-- Row 3: Parsed (Proto) Full-width Card -->
  <div class="rounded-xl border border-border/80 bg-dark-card overflow-hidden">
    <div class="flex items-center gap-2 border-b border-border/60 bg-panel2/50 px-4 py-2.5">
      <span class="text-accent2">
        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="2" y="2" width="12" height="12" rx="3" />
          <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
        </svg>
      </span>
      <span class="font-mono text-[12px] font-semibold text-text">
        Parsed <span class="text-muted font-normal">(Proto)</span>
      </span>
    </div>

    <div class="grid grid-cols-1 gap-6 p-4 lg:grid-cols-2">
      <!-- Request Message -->
      <div>
        <div class="mb-2 text-[12px] font-medium text-muted">Request message</div>
        <div class="rounded-lg border border-border/50 bg-panel/60 p-3 font-mono text-[12px] leading-relaxed">
          <pre class="m-0 whitespace-pre">{@html formatProtoStruct(requestJsonData)}</pre>
        </div>
      </div>

      <!-- Response Message -->
      <div>
        <div class="mb-2 text-[12px] font-medium text-muted">Response message</div>
        <div class="rounded-lg border border-border/50 bg-panel/60 p-3 font-mono text-[12px] leading-relaxed">
          <pre class="m-0 whitespace-pre">{@html formatProtoStruct(responseJsonData)}</pre>
        </div>
      </div>
    </div>
  </div>
</div>
