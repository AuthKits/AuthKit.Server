<script lang="ts">
  import type { GrpcService } from "../../lib/types";
  import { motion } from "@humanspeak/svelte-motion";
  import { protoPreview } from "../../lib/format/format";
  import { protoCodeLines } from "../../lib/proto/protoView";
  import { useClipboard } from "../../lib/clipboard/useClipboard.svelte";

  let {
    service,
    isOpen,
    onClose,
  }: {
    service: GrpcService;
    isOpen: boolean;
    onClose: () => void;
  } = $props();

  const { copiedSection: copied, copy } = useClipboard();

  const protoCode = $derived(protoPreview(service));
  const lines = $derived(protoCodeLines(protoCode));

  function handleKeydown(e: KeyboardEvent): void {
    if (e.key === "Escape") {
      onClose();
    }
  }
</script>

<svelte:window onkeydown={handleKeydown} />

{#if isOpen}
  <motion.div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
    role="dialog"
    aria-modal="true"
    initial={{ opacity: 0 }}
    animate={{ opacity: 1 }}
    exit={{ opacity: 0 }}
    transition={{ duration: 0.2 }}
  >
    <!-- Backdrop Click Area -->
    <motion.button
      type="button"
      class="fixed inset-0 h-full w-full cursor-default bg-transparent border-0"
      onclick={onClose}
      aria-label="Close modal"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.2 }}
    ></motion.button>

    <!-- Modal Box -->
    <motion.div
      class="relative z-10 flex max-h-[85vh] w-full max-w-4xl flex-col rounded-2xl border border-accent/20 bg-modal-dark shadow-2xl overflow-hidden"
      initial={{ opacity: 0, scale: 0.9, y: 20 }}
      animate={{ opacity: 1, scale: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.9, y: 20 }}
      transition={{ duration: 0.3, type: "spring", stiffness: 300 }}
    >
      <!-- Modal Header -->
      <motion.div
        class="flex items-center justify-between border-b border-accent/10 bg-gradient-to-r from-panel to-panel2 px-6 py-4"
        initial={{ opacity: 0, y: -10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, delay: 0.1 }}
      >
        <div class="flex items-center gap-3">
          <motion.div
            class="flex size-8 items-center justify-center rounded-xl bg-accent/15 text-accent shadow-lg shadow-accent/20"
            animate={{ 
              rotate: [0, 5, -5, 0],
              scale: [1, 1.05, 1]
            }}
            transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
          >
            <svg
              width="18"
              height="18"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              stroke-width="1.5"
            >
              <rect x="2" y="2" width="12" height="12" rx="3" />
              <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
            </svg>
          </motion.div>
          <div>
            <motion.h3 
              class="m-0 text-[15px] font-bold text-text"
              whileHover={{ letterSpacing: "0.05em" }}
              transition={{ type: "spring", stiffness: 300 }}
            >
              {service.name}.proto
            </motion.h3>
            <p class="m-0 font-mono text-[11px] text-accent2">
              package {service.package};
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <!-- Copy Button -->
          <motion.button
            type="button"
            class="flex cursor-pointer items-center gap-2 rounded-xl border border-accent/20 bg-accent/10 px-4 py-2 text-[12px] font-semibold text-accent hover:bg-accent/15 hover:border-accent/30"
            onclick={() => copy(protoCode, "proto")}
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.3, delay: 0.2 }}
            whileHover={{ scale: 1.05, y: -2 }}
            whileTap={{ scale: 0.95 }}
          >
            {#if copied !== null}
              <motion.svg
                width="14"
                height="14"
                viewBox="0 0 16 16"
                fill="none"
                stroke="var(--color-green)"
                stroke-width="2"
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ type: "spring", stiffness: 300 }}
              >
                <path
                  d="M3.5 8.5 6.5 11.5 12.5 4.5"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                />
              </motion.svg>
              <span class="text-green">Copied!</span>
            {:else}
              <svg
                width="14"
                height="14"
                viewBox="0 0 16 16"
                fill="none"
                stroke="currentColor"
                stroke-width="1.5"
              >
                <rect x="5" y="5" width="8" height="8" rx="1.5" />
                <path d="M3 11V3a1 1 0 0 1 1-1h8" stroke-linecap="round" />
              </svg>
              <span>Copy Proto</span>
            {/if}
          </motion.button>

          <!-- Close Button -->
          <motion.button
            type="button"
            class="flex size-9 cursor-pointer items-center justify-center rounded-xl border border-border/50 bg-panel2 text-muted hover:bg-panel hover:text-text"
            onclick={onClose}
            aria-label="Close"
            initial={{ opacity: 0, x: 20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.3, delay: 0.25 }}
            whileHover={{ scale: 1.1, rotate: 90, backgroundColor: "rgba(239, 68, 68, 0.1)", borderColor: "rgba(239, 68, 68, 0.3)" }}
            whileTap={{ scale: 0.9 }}
          >
            <svg
              width="16"
              height="16"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path
                d="M4 4l8 8M12 4l-8 8"
                stroke-linecap="round"
                stroke-linejoin="round"
              />
            </svg>
          </motion.button>
        </div>
      </motion.div>

      <!-- Code viewer with line numbers -->
      <motion.div
        class="flex min-h-0 flex-1 overflow-auto bg-dark-card p-5 font-mono text-[13px] leading-[24px]"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 0.3, delay: 0.3 }}
      >
        <div
          class="shrink-0 select-none border-r border-accent/10 pr-5 text-right text-muted/50 font-mono text-[12px]"
        >
          {#each lines as line, index}
            <motion.div
              class="hover:text-accent/80 transition-colors cursor-pointer"
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.2, delay: 0.4 + (index * 0.01) }}
            >
              {line.number}
            </motion.div>
          {/each}
        </div>
        <div class="min-w-0 flex-1 pl-5 text-text">
          {#each lines as line, index}
            <motion.div
              class="whitespace-pre hover:bg-accent/5 rounded px-1 -mx-1 transition-colors"
              initial={{ opacity: 0, x: 10 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.2, delay: 0.4 + (index * 0.01) }}
            >
              {@html line.html || "&nbsp;"}
            </motion.div>
          {/each}
        </div>
      </motion.div>
      
      <!-- Footer with stats -->
      <motion.div
        class="flex items-center justify-between border-t border-accent/10 bg-panel px-5 py-3 text-[11px] text-muted"
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, delay: 0.5 }}
      >
        <div class="flex items-center gap-4">
          <span class="flex items-center gap-1.5">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <path d="M4 12h8M4 8h8M4 4h8" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            {lines.length} lines
          </span>
          <span class="flex items-center gap-1.5">
            <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
              <circle cx="8" cy="8" r="6" />
              <path d="M8 5v3l2 2" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
            {service.methods.length} methods
          </span>
        </div>
        <div class="flex items-center gap-2">
          <kbd class="rounded border border-border/50 bg-panel2 px-2 py-0.5 font-mono text-[10px]">ESC</kbd>
          <span>to close</span>
        </div>
      </motion.div>
    </motion.div>
  </motion.div>
{/if}