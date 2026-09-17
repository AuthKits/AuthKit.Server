<script lang="ts">
  import { requestHeaders, response } from "../../lib/stores";
  import {
    formatMetadata,
    parseMetadata,
    parseRequestMetadata,
  } from "../../lib/metadata/metadata";
  import { useClipboard } from "../../lib/clipboard/useClipboard.svelte";

  const { copiedSection, copy } = useClipboard();

  /** Parsed view of the typed request headers (what will be sent). */
  const requestMetadata = $derived(parseRequestMetadata($requestHeaders));

  /**
   * Response metadata received from the server, built from the invocation
   * result's `metadata` and `trailers`. gRPC-standard keys are surfaced first
   * (with safe fallbacks) so the row order is stable regardless of the wire
   * shape. No values are hardcoded — every entry comes from `$response`.
   */
  const responseMetadata = $derived(
    $response === null
      ? []
      : parseMetadata($response.metadata, $response.trailers, {
          status: $response.status,
          statusName: $response.statusName,
          statusCode: $response.statusCode,
          contentType: $response.contentType,
        }),
  );

  function copyRequest(): void {
    void copy($requestHeaders, "request");
  }

  function copyResponse(): void {
    void copy(formatMetadata(responseMetadata), "response");
  }
</script>

<div class="flex flex-col gap-5 overflow-y-auto p-1">
  <!-- Card 1: Request metadata (editable, persisted) -->
  <div class="rounded-xl border border-border/80 bg-bg overflow-hidden">
    <!-- Card Top Header -->
    <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-3">
      <div class="flex items-center gap-2.5">
        <span class="flex size-6 items-center justify-center rounded-full bg-accent/15 text-accent ring-1 ring-accent/30">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 12h8M4 8h8M4 4h8" stroke-linecap="round" />
          </svg>
        </span>
        <span class="text-[14px] font-bold text-text">Request metadata</span>
      </div>

      <div class="flex items-center gap-4 text-[12px] text-muted font-mono">
        <span class="flex items-center gap-1.5">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="6" cy="6" r="4" />
            <path d="M6 2v4H2m0 0l2 2m-2-2 2-2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>{requestMetadata.length} header{requestMetadata.length === 1 ? "" : "s"}</span>
        </span>
        <span class="flex items-center gap-1.5">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="1.5">
            <path d="M3 12.5h10v-1a2 2 0 0 0-2-2H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v1h2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>session storage</span>
        </span>
      </div>
    </div>

    <!-- Editor -->
    <div class="p-4 pt-3">
      <p class="m-0 mb-2 text-[11px] text-muted">
        Type gRPC metadata as <code class="font-mono text-text/80">Key: Value</code>, one header per line.
        Persisted across method switches.
      </p>
      <textarea
        bind:value={$requestHeaders}
        placeholder="authorization: Bearer &lt;token&gt;&#10;x-request-id: abc-123&#10;grpc-encoding: gzip"
        spellcheck="false"
        autocomplete="off"
        class="w-full min-h-[110px] resize-y rounded-lg border border-border bg-bg font-mono text-[12.5px] text-text placeholder-muted/50 p-3 outline-none focus:border-accent"
      ></textarea>
    </div>

    <!-- Parsed preview -->
    {#if requestMetadata.length > 0}
      <div class="flex items-center justify-between border-t border-border/40 px-4 py-2">
        <span class="text-[10px] font-semibold uppercase tracking-wider text-muted">Parsed preview</span>
        <button
          type="button"
          class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={copyRequest}
        >
          {#if copiedSection === "request"}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="2">
              <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            <span class="text-green">Copied</span>
          {:else}
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <rect x="5" y="5" width="8" height="8" rx="1.5" />
              <path d="M3 11V3a1 1 0 0 1 1-1h8" stroke-linecap="round" />
            </svg>
            <span>Copy raw</span>
          {/if}
        </button>
      </div>
      <div class="relative overflow-x-auto px-4 pb-4">
        <table class="w-full border-collapse">
          <thead>
            <tr class="border-b border-border/40 text-left text-[11px] font-semibold text-muted">
              <th class="w-12 px-3 py-2">#</th>
              <th class="px-3 py-2 font-sans">Key</th>
              <th class="px-3 py-2 font-sans">Value</th>
            </tr>
          </thead>
          <tbody>
            {#each requestMetadata as [key, value], i}
              <tr class="border-b border-border/30 last:border-b-0 hover:bg-panel2/30 transition-colors">
                <td class="px-3 py-2 text-center font-mono text-[11px] text-muted/50 select-none">{i + 1}</td>
                <td class="px-3 py-2 font-mono text-[12px] text-accent2">{key}</td>
                <td class="px-3 py-2 font-mono text-[12px] text-text/90">{value === "" ? '""' : value}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>

  <!-- Card 2: Response metadata (read-only, from $response) -->
  <div class="rounded-xl border border-border/80 bg-bg overflow-hidden">
    <!-- Card Top Header -->
    <div class="flex items-center justify-between border-b border-border/60 bg-panel2/50 px-4 py-3">
      <div class="flex items-center gap-2.5">
        <span class="flex size-6 items-center justify-center rounded-full bg-green/15 text-green ring-1 ring-green/30">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </span>
        <span class="text-[14px] font-bold text-text">Response metadata</span>
      </div>

      <div class="flex items-center gap-4 text-[12px] text-muted font-mono">
        <span class="flex items-center gap-1.5">
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="8" cy="8" r="6" />
            <path d="M8 5v3l2 1" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>{$response === null ? "—" : `${responseMetadata.length} header${responseMetadata.length === 1 ? "" : "s"}`}</span>
        </span>
      </div>
    </div>

    {#if $response === null}
      <div class="p-5">
        <div class="flex items-center gap-2.5 text-[13px] text-muted">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="8" cy="8" r="6" />
            <path d="M8 5v3l2 1" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span>Invoke a method to inspect the metadata returned by the server.</span>
        </div>
      </div>
    {:else}
      <div class="flex items-center justify-between border-t border-border/40 px-4 py-2">
        <span class="text-[10px] font-semibold uppercase tracking-wider text-muted">Received headers</span>
        <button
          type="button"
          class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2.5 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
          onclick={copyResponse}
        >
          {#if copiedSection === "response"}
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
      <div class="relative overflow-x-auto px-4 pb-4">
        <table class="w-full border-collapse">
          <thead>
            <tr class="border-b border-border/40 text-left text-[11px] font-semibold text-muted">
              <th class="w-12 px-3 py-2 text-center font-mono">#</th>
              <th class="w-[220px] px-3 py-2 font-sans">Key</th>
              <th class="px-3 py-2 font-sans">Value</th>
              <th class="w-24 px-3 py-2 text-right">Copy</th>
            </tr>
          </thead>
          <tbody>
            {#each responseMetadata as [key, value], i}
              <tr class="border-b border-border/30 last:border-b-0 hover:bg-panel2/30 transition-colors">
                <td class="px-3 py-2 text-center font-mono text-[11px] text-muted/50 select-none">{i + 1}</td>
                <td class="px-3 py-2 font-mono text-[12px] text-accent2">{key}</td>
                <td class="px-3 py-2 font-mono text-[12px] text-text/90">{value === "" ? '""' : value}</td>
                <td class="px-3 py-2 text-right">
                  <button
                    type="button"
                    class="inline-flex cursor-pointer items-center gap-1.5 rounded-md border border-border bg-panel px-2 py-1 text-[11px] text-muted transition-colors hover:bg-panel2 hover:text-text"
                    onclick={() => copy(formatMetadata([[key, value]]), `row-${i}`)}
                  >
                    {#if copiedSection === `row-${i}`}
                      <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="var(--color-green)" stroke-width="2">
                        <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
                      </svg>
                      <span class="text-green">Copied</span>
                    {:else}
                      <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
                        <rect x="3.5" y="3.5" width="6" height="6" rx="1" />
                        <path d="M3 9.5l2.5 2.5L9 8" stroke-linecap="round" stroke-linejoin="round" />
                      </svg>
                    {/if}
                  </button>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>
</div>
