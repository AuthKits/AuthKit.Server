<script lang="ts">
  import { connectionStatus, connectedTarget, theme } from "../../lib/stores";
  import { motion } from "@humanspeak/svelte-motion";

  const palette = ["#a66b3d", "#c08d54", "#8a552f"];

  function toggleTheme(): void {
    $theme = $theme === "dark" ? "light" : "dark";
  }
</script>

<motion.header
  class="flex h-14 shrink-0 items-center justify-between border-b border-border bg-panel px-4"
  initial={{ opacity: 0, y: -10 }}
  animate={{ opacity: 1, y: 0 }}
  transition={{ duration: 0.3, ease: "easeOut" }}
>
  <!-- Left Brand -->
  <motion.div 
    class="flex items-center gap-3"
    initial={{ opacity: 0, x: -10 }}
    animate={{ opacity: 1, x: 0 }}
    transition={{ duration: 0.3, delay: 0.05 }}
  >
    <motion.svg
      width="28"
      height="28"
      viewBox="0 0 32 32"
      aria-hidden="true"
      class="shrink-0"
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
    >
      <defs>
        <linearGradient id="logo-gradient" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color={palette[0]} />
          <stop offset="1" stop-color={palette[2]} />
        </linearGradient>
      </defs>
      <polygon
        points="16,2 28,9 28,23 16,30 4,23 4,9"
        fill="url(#logo-gradient)"
      />
      <polygon
        points="16,2 28,9 28,23 16,30 4,23 4,9"
        fill="none"
        stroke="#ffffff33"
        stroke-width="1.5"
      />
      <path
        d="M16 8l8 4.6v9.2L16 26.4 8 21.8v-9.2L16 8z"
        fill="none"
        stroke="#ffffff"
        stroke-width="1.5"
      />
    </motion.svg>
    <div class="flex min-w-0 flex-col">
      <span class="grpc-brand-shimmer text-[15px] font-bold leading-tight tracking-tight">gRPC UI</span>
      <span class="text-[11px] leading-tight text-muted">Explore. Invoke. Debug.</span>
    </div>
  </motion.div>

  <!-- Right Actions -->
  <motion.div 
    class="flex items-center gap-3"
    initial={{ opacity: 0, x: 10 }}
    animate={{ opacity: 1, x: 0 }}
    transition={{ duration: 0.3, delay: 0.1 }}
  >
    <motion.span
      class="inline-flex items-center gap-1.5 rounded-full border border-green/40 bg-green/15 px-2.5 py-1 text-[11px] font-semibold text-green ring-1 ring-inset ring-green/20"
      whileHover={{ scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
    >
      {#if $connectionStatus === "connecting"}
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0 animate-spin">
          <circle cx="8" cy="8" r="6" opacity="0.3" />
          <path d="M14 8a6 6 0 0 0-6-6" stroke-linecap="round" />
        </svg>
        Connecting…
      {:else if $connectionStatus === "error"}
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0">
          <circle cx="8" cy="8" r="6.5" />
          <path d="M8 4.5V9M8 11v.5" stroke-linecap="round" />
        </svg>
        Disconnected
      {:else}
        <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" class="shrink-0">
          <path d="M3.5 8.5 6.5 11.5 12.5 4.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        Connected
      {/if}
    </motion.span>

    <motion.span
      class="hidden items-center gap-1.5 rounded-full border border-border bg-panel2/60 dark:bg-dark-card/70 px-3 py-1 font-mono text-[11px] text-muted sm:inline-flex"
      whileHover={{ scale: 1.02 }}
      whileTap={{ scale: 0.98 }}
    >
      <svg width="11" height="11" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M6 10 9 7 12 10M3.5 8H1.5M14.5 8H12.5M8 3.5V1.5M8 14.5V12.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
      {$connectedTarget}
      <svg width="10" height="10" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
        <path d="M4 12l8-8M6 4h6v6" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </motion.span>

    <div class="flex items-center gap-1 border-l border-border pl-2 text-muted">
      <!-- Theme Toggle -->
      <motion.button
        type="button"
        title="Toggle Theme"
        onclick={toggleTheme}
        class="flex size-8 cursor-pointer items-center justify-center rounded-lg hover:bg-panel2 hover:text-text transition-colors"
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
      >
        <svg width="15" height="15" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5">
          {#if $theme === "dark"}
            <path d="M13.5 9.5a6 6 0 1 1-7-7 6 6 0 0 0 7 7z" stroke-linecap="round" stroke-linejoin="round" />
          {:else}
            <path d="M12 2a1 1 0 0 1 1 1v1a1 1 0 0 1-2 0V3a1 1 0 0 1 2-2zm5.657 2.343a1 1 0 0 1 0 1.414l-.707.707a1 1 0 1 1-1.414-1.414l.707-.707a1 1 0 0 1 1.414 0zm-11.314 0a1 1 0 0 1 0-1.414l.707-.707a1 1 0 1 1 1.414 1.414l-.707.707a1 1 0 0 1-1.414 0zM4 12a1 1 0 0 1 0 2H3a1 1 0 0 1 0-2h1zm8 0a1 1 0 0 1 0 2h1a1 1 0 0 1 0-2h-1zm4.657 2.343a1 1 0 0 1 1.414 0l.707-.707a1 1 0 0 1 1.414 0l.707-.707a1 1 0 0 1 0 1.414l-.707.707a1 1 0 0 1-1.414 0l-.707.707a1 1 0 0 1 0-1.414zM12 15a1 1 0 0 1 2 0v1a1 1 0 0 1-2 0v-1zm7-5.657a1 1 0 0 1 0 1.414l.707.707a1 1 0 1 1-1.414 1.414l-.707-.707a1 1 0 0 1 1.414-1.414l.707.707zM5.657 15a1 1 0 0 1 0-1.414l.707-.707a1 1 0 1 1 1.414 1.414l-.707.707a1 1 0 0 1-1.414 0zM3 12a1 1 0 0 1 1-1h1a1 1 0 0 1 0 2H4a1 1 0 0 1-1-1z" stroke-linecap="round" stroke-linejoin="round" />
          {/if}
        </svg>
      </motion.button>
    </div>
  </motion.div>
</motion.header>