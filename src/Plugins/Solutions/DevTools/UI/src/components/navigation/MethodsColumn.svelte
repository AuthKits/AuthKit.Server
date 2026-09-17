<script lang="ts">
  import {
    selectedMethodName,
    selectedService,
    selectedServiceName,
  } from "../../lib/stores";
  import { motion } from "@humanspeak/svelte-motion";
  import ProtoModal from "../proto/ProtoModal.svelte";

  let isProtoOpen = $state(false);

  function selectMethod(methodName: string): void {
    $selectedMethodName = methodName;
  }
</script>

{#if $selectedService}
  {@const service = $selectedService}
  <motion.aside
    class="flex w-[260px] shrink-0 flex-col border-r border-border bg-panel"
    initial={{ opacity: 0, x: -80, scale: 0.9, rotateY: -10 }}
    animate={{ opacity: 1, x: 0, scale: 1, rotateY: 0 }}
    transition={{ duration: 0.6, delay: 0.4, type: "spring", stiffness: 150 }}
  >
    <!-- Service Header -->
    <motion.div 
      class="border-b border-border p-3.5"
      initial={{ opacity: 0, y: -40, scale: 0.8 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ duration: 0.5, delay: 0.5, type: "spring", stiffness: 200 }}
    >
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2.5 min-w-0">
          <motion.span
            class="flex size-7 shrink-0 items-center justify-center rounded-lg bg-accent/15 text-accent"
            animate={{ 
              rotate: [0, 5, -5, 0],
              scale: [1, 1.05, 1]
            }}
            transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
            whileHover={{ scale: 1.3, rotate: 15, filter: "brightness(1.2)" }}
            whileTap={{ scale: 0.8 }}
          >
            <svg
              width="16"
              height="16"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              stroke-width="1.5"
            >
              <rect x="2" y="2" width="12" height="12" rx="3" />
              <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
            </svg>
          </motion.span>
          <motion.div 
            class="min-w-0 flex-1"
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.4, delay: 0.6 }}
          >
            <motion.h3 
              class="m-0 truncate text-[13px] font-bold text-text" 
              title={service.name}
              whileHover={{ letterSpacing: "0.05em", scale: 1.02 }}
              transition={{ type: "spring", stiffness: 300 }}
            >
              {service.name}
            </motion.h3>
            <p class="m-0 truncate text-[11px] text-muted font-mono" title={service.package}>
              {service.package}
            </p>
          </motion.div>
        </div>
      </div>

      <!-- Quick Action to view Proto -->
      <motion.button
        type="button"
        class="mt-3 flex w-full cursor-pointer items-center justify-center gap-2 rounded-xl bg-accent/10 border border-accent/20 px-3 py-2.5 text-[12px] font-semibold text-accent shadow-lg"
        onclick={() => (isProtoOpen = true)}
        initial={{ opacity: 0, y: 20, scale: 0.9 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ duration: 0.5, delay: 0.8, type: "spring", stiffness: 200 }}
        whileHover={{ 
          scale: 1.03, 
          y: -3, 
          backgroundColor: "rgba(166, 107, 61, 0.15)",
          borderColor: "rgba(166, 107, 61, 0.4)",
          boxShadow: "0 8px 25px rgba(166, 107, 61, 0.15)"
        }}
        whileTap={{ scale: 0.97, y: 0 }}
      >
        <motion.div 
          class="flex size-5 items-center justify-center rounded-lg bg-accent/20"
          animate={{ 
            rotate: [0, 10, -10, 0],
            scale: [1, 1.1, 1]
          }}
          transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
        >
          <svg width="12" height="12" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M4 12h8M4 8h8M4 4h8" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </motion.div>
        <span class="flex items-center gap-1">
          <span>Proto Schema</span>
          <motion.span
            class="text-[9px] opacity-70"
            animate={{ opacity: [0.5, 1, 0.5] }}
            transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
          >
            →
          </motion.span>
        </span>
      </motion.button>
    </motion.div>

    <!-- Methods Section -->
    <motion.div 
      class="flex flex-1 flex-col overflow-y-auto p-3"
      initial={{ opacity: 0, y: 30 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5, delay: 0.9, type: "spring", stiffness: 150 }}
    >
      <motion.div 
        class="mb-2 px-1 text-[11px] font-semibold uppercase tracking-wider text-muted"
        animate={{ letterSpacing: ["0.05em", "0.15em", "0.05em"] }}
        transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
      >
        Methods
      </motion.div>

      <div class="flex flex-col gap-1">
        {#each service.methods as method, index (method.name)}
          {@const isSelected = $selectedMethodName === method.name}
          <motion.button
            type="button"
            class="flex w-full cursor-pointer items-center justify-between rounded-lg px-3 py-2 text-left transition-colors {isSelected
              ? 'bg-accent text-white font-medium shadow-sm'
              : 'text-text hover:bg-panel2/80'}"
            onclick={() => selectMethod(method.name)}
            initial={{ opacity: 0, x: -30, scale: 0.9 }}
            animate={{ opacity: 1, x: 0, scale: 1 }}
            transition={{ duration: 0.3, delay: 1.0 + (index * 0.1), type: "spring", stiffness: 200 }}
            whileHover={{ scale: 1.05, x: 5 }}
            whileTap={{ scale: 0.95, x: 0 }}
          >
            <div class="flex items-center gap-2 min-w-0">
              <motion.span
                class="shrink-0 rounded px-1 py-0.5 font-mono text-[9px] font-bold uppercase tracking-wider {isSelected
                  ? 'bg-white text-accent'
                  : 'bg-accent/15 text-accent'}"
                animate={{ 
                  scale: [1, 1.1, 1],
                  rotate: [0, 2, -2, 0]
                }}
                transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
              >
                RPC
              </motion.span>
              <span class="truncate font-mono text-[12.5px]">{method.name}</span>
            </div>
            <motion.svg
              width="12"
              height="12"
              viewBox="0 0 16 16"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              class="shrink-0 {isSelected ? 'text-white' : 'text-muted'}"
              animate={{ x: [0, 2, 0] }}
              transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
            >
              <path d="m6 4 4 4-4 4" stroke-linecap="round" stroke-linejoin="round" />
            </motion.svg>
          </motion.button>
        {/each}
      </div>
    </motion.div>
  </motion.aside>

  <!-- Full Proto Modal Dialog -->
  <ProtoModal
    {service}
    isOpen={isProtoOpen}
    onClose={() => (isProtoOpen = false)}
  />
{/if}