<script lang="ts">
  import { onMount } from "svelte";
  import { motion } from "@humanspeak/svelte-motion";
  import { selectedMethodName, selectedServiceName, services } from "../../lib/stores";
  import { isSearchShortcut } from "../../lib/navigation/navigation";
  import ServiceTree from "./ServiceTree.svelte";

  let search = $state("");
  let input: HTMLInputElement;

  function applySearch(action: "clear" | "keep"): void {
    $selectedServiceName = null;
    $selectedMethodName = null;
    if (action === "clear") search = "";
    input?.focus();
  }

  onMount(() => {
    function onKeydown(event: KeyboardEvent): void {
      if (isSearchShortcut(event)) {
        event.preventDefault();
        input?.focus();
      }
    }
    window.addEventListener("keydown", onKeydown);
    return () => window.removeEventListener("keydown", onKeydown);
  });
</script>

<motion.aside 
  class="flex w-[300px] shrink-0 flex-col border-r border-border bg-panel"
  initial={{ opacity: 0, x: -10 }}
  animate={{ opacity: 1, x: 0 }}
  transition={{ duration: 0.3, delay: 0.1 }}
>
  <motion.div 
    class="shrink-0 p-3"
    initial={{ opacity: 0, y: -5 }}
    animate={{ opacity: 1, y: 0 }}
    transition={{ duration: 0.2, delay: 0.15 }}
  >
    <div class="relative">
      <svg
        width="14"
        height="14"
        viewBox="0 0 16 16"
        fill="none"
        stroke="var(--color-muted)"
        stroke-width="1.5"
        class="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2"
      >
        <circle cx="7" cy="7" r="4.5" />
        <path d="m10.5 10.5 3 3" stroke-linecap="round" />
      </svg>
      <input
        bind:this={input}
        bind:value={search}
        type="text"
        placeholder="Search services..."
        class="grpc-input pl-8 pr-14"
      />
      <kbd
        class="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 rounded border border-border bg-panel2 px-1.5 font-mono text-[10px] text-muted"
      >
        ⌘K
      </kbd>
    </div>
  </motion.div>

  <motion.div 
    class="flex shrink-0 items-center justify-between px-4 pb-2 pt-1"
    initial={{ opacity: 0 }}
    animate={{ opacity: 1 }}
    transition={{ duration: 0.2, delay: 0.2 }}
  >
    <span class="text-[11px] font-semibold uppercase tracking-wider text-muted">
      Services ({$services.length})
    </span>
    {#if search}
      <motion.button
        type="button"
        class="cursor-pointer border-0 bg-transparent px-1 text-[11px] text-muted hover:text-text"
        onclick={() => applySearch("clear")}
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
      >
        Clear
      </motion.button>
    {/if}
  </motion.div>

  <motion.div 
    class="min-h-0 flex-1 overflow-y-auto px-2 pb-3"
    initial={{ opacity: 0 }}
    animate={{ opacity: 1 }}
    transition={{ duration: 0.2, delay: 0.25 }}
  >
    <ServiceTree {search} />
  </motion.div>
</motion.aside>
