<script lang="ts">
  import ActionBar from "./ActionBar.svelte";
  import JsonEditor from "./JsonEditor.svelte";
  import MetadataTable from "./MetadataTable.svelte";
  import MethodHeader from "./MethodHeader.svelte";
  import MethodTabs from "./MethodTabs.svelte";
  import ProtoView from "../proto/ProtoView.svelte";
  import ResponsePanel from "./ResponsePanel.svelte";
  import { motion } from "@humanspeak/svelte-motion";
  import { api, activeTab, requestHeaders, requestJson, response, selectedMethod, selectedMethodName, selectedService } from "../../lib/stores";
  import { friendlyBytes, formatError, prettyPrint } from "../../lib/format/format";
  import { buildInvokeRequest, initialMethodState } from "../../lib/api/invoke";

  let statusMessage = $state<string | null>(null);
  let isInvoking = $state(false);
  let payloadSizeBytes = $state(0);

  function loadMethodDefaults(): void {
    const reset = initialMethodState($selectedService, $selectedMethodName);
    if (reset === null) return;
    $requestJson = reset.requestJson;
    $response = null;
    $activeTab = "request";
    statusMessage = null;
    isInvoking = false;
    payloadSizeBytes = 0;
  }

  async function invoke(): Promise<void> {
    if ($selectedMethod === null) return;
    isInvoking = true;
    statusMessage = null;
    try {
      $response = await api.invoke(
        buildInvokeRequest($selectedMethod, $requestJson, $requestHeaders),
      );
      $activeTab = "response";
    } catch (err: unknown) {
      statusMessage = `Invocation failed: ${formatError(err)}`;
    } finally {
      isInvoking = false;
    }
  }

  function fillDefaults(): void {
    loadMethodDefaults();
  }

  function formatJson(): void {
    $requestJson = prettyPrint($requestJson);
  }

  function clearJson(): void {
    $requestJson = "";
    $response = null;
  }

  $effect(() => {
    void $selectedMethodName;
    void $selectedService;
    loadMethodDefaults();
  });
</script>

{#if $selectedService === null || $selectedMethodName === null}
  <motion.div 
    class="flex h-full flex-1 flex-col items-center justify-center gap-3 p-8 text-center"
    initial={{ opacity: 0, scale: 0.5, rotateY: -20 }}
    animate={{ opacity: 1, scale: 1, rotateY: 0 }}
    transition={{ duration: 0.8, delay: 0.8, type: "spring", stiffness: 100 }}
  >
    <motion.div 
      class="flex size-16 items-center justify-center rounded-2xl bg-panel2 border-2 border-border/70 text-muted shadow-sm"
      animate={{ 
        scale: [1, 1.1, 1],
        rotate: [0, 5, -5, 0],
        y: [0, -10, 0]
      }}
      transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
      whileHover={{ scale: 1.3, rotate: 15, filter: "brightness(1.2)", boxShadow: "0 0 30px rgba(0, 0, 0, 0.2)" }}
      whileTap={{ scale: 0.8 }}
    >
      <motion.svg 
        width="32" 
        height="32" 
        viewBox="0 0 16 16" 
        fill="none" 
        stroke="currentColor" 
        stroke-width="1.5"
        animate={{ rotate: 360 }}
        transition={{ duration: 30, repeat: Infinity, ease: "linear" }}
      >
        <circle cx="8" cy="8" r="6" />
        <path d="M8 5v3l2 2" stroke-linecap="round" stroke-linejoin="round" />
      </motion.svg>
    </motion.div>
    <motion.p 
      class="m-0 text-[13px] text-muted"
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: [0.7, 1, 0.7], y: 0 }}
      transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
    >
      Pick an RPC method from the sidebar to start testing.
    </motion.p>
  </motion.div>
{:else}
  {@const service = $selectedService}
  {@const method = service.methods.find((m) => m.name === $selectedMethodName)}
  {#if method}
    <motion.section 
      class="flex h-full min-w-0 flex-1 flex-col overflow-hidden bg-bg"
      initial={{ opacity: 0, y: 50, scale: 0.95, rotateX: 5 }}
      animate={{ opacity: 1, y: 0, scale: 1, rotateX: 0 }}
      transition={{ duration: 0.7, delay: 0.8, type: "spring", stiffness: 120 }}
    >
      <motion.div 
        class="shrink-0 border-b border-border bg-panel2/20 p-5"
        initial={{ opacity: 0, y: -30, scale: 0.9 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        transition={{ duration: 0.5, delay: 0.9, type: "spring", stiffness: 200 }}
      >
        <MethodHeader {service} {method} />
      </motion.div>

      <motion.div 
        class="shrink-0"
        initial={{ opacity: 0, y: -20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, delay: 1.0, type: "spring", stiffness: 200 }}
      >
        <MethodTabs />
      </motion.div>

      <motion.div 
        class="min-h-0 flex-1 overflow-y-auto"
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, delay: 1.1, type: "spring", stiffness: 150 }}
      >
        {#if $activeTab === "request"}
          {#if method.requestFields !== undefined && method.requestFields.length === 0}
            <motion.div 
              class="flex h-full min-h-[300px] flex-col items-center justify-center gap-4 p-8 text-center"
              initial={{ opacity: 0, scale: 0.7, rotate: -10 }}
              animate={{ opacity: 1, scale: 1, rotate: 0 }}
              transition={{ duration: 0.6, type: "spring", stiffness: 150 }}
            >
              <motion.div 
                class="flex size-16 items-center justify-center rounded-2xl bg-panel2 border border-border/80 text-muted shadow-sm"
                animate={{ 
                  scale: [1, 1.2, 1],
                  rotate: [0, 10, -10, 0],
                  y: [0, -15, 0]
                }}
                transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
                whileHover={{ scale: 1.4, rotate: 20, filter: "brightness(1.3)", boxShadow: "0 0 40px rgba(0, 0, 0, 0.3)" }}
                whileTap={{ scale: 0.7 }}
              >
                <motion.svg 
                  width="36" 
                  height="36" 
                  viewBox="0 0 16 16" 
                  fill="none" 
                  stroke="currentColor" 
                  stroke-width="1.5"
                  animate={{ 
                    pathLength: [0, 1],
                    opacity: [0.5, 1, 0.5]
                  }}
                  transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
                >
                  <path d="M3 8h4l2-3 2 6 2-3h2" stroke-linecap="round" stroke-linejoin="round" />
                </motion.svg>
              </motion.div>
              <motion.div 
                class="max-w-md"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.5, delay: 0.3 }}
              >
                <p class="m-0 text-[13px] text-muted leading-relaxed">
                  This method takes an empty request (<code class="font-mono text-text bg-panel2 px-1.5 py-0.5 rounded border border-border/80">google.protobuf.Empty</code>) — nothing to configure.
                </p>
              </motion.div>

              <!-- Also show action bar for empty request methods -->
              <motion.div
                initial={{ opacity: 0, y: 30, scale: 0.95 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                transition={{ duration: 0.5, delay: 0.2, type: "spring", stiffness: 200 }}
              >
                <ActionBar
                  {isInvoking}
                  onInvoke={invoke}
                  onFillDefaults={fillDefaults}
                  onFormatJson={formatJson}
                  onClear={clearJson}
                />
                {#if statusMessage !== null}
                  <motion.div
                    class="shrink-0 px-3 pb-2 text-[12px] text-red"
                    initial={{ opacity: 0, x: -20 }}
                    animate={{
                      x: [0, 5, -5, 0],
                      opacity: [0.8, 1, 0.8]
                    }}
                    transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                  >
                    {statusMessage}
                  </motion.div>
                {/if}
              </motion.div>
            </motion.div>
          {:else}
            <motion.div 
              class="flex h-full min-h-0 flex-col gap-3 p-5"
              initial={{ opacity: 0, y: 30, scale: 0.95 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              transition={{ duration: 0.5, type: "spring", stiffness: 200 }}
            >
              <motion.div 
                class="flex shrink-0 items-center gap-2"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.4, delay: 0.2 }}
              >
                <motion.h3 
                  class="m-0 text-[13px] font-bold"
                  whileHover={{ letterSpacing: "0.1em", scale: 1.05 }}
                  transition={{ type: "spring", stiffness: 300 }}
                >
                  Request JSON
                </motion.h3>
                <motion.span 
                  class="ml-auto font-mono text-[11px] text-muted"
                  animate={{ 
                    scale: [1, 1.1, 1],
                    opacity: [0.7, 1, 0.7]
                  }}
                  transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                >
                  {friendlyBytes(payloadSizeBytes)}
                </motion.span>
              </motion.div>
              <motion.div
                class="min-h-0"
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.4, delay: 0.3 }}
              >
                <JsonEditor
                  bind:payloadSizeBytes
                  value={$requestJson}
                  onChange={(next) => ($requestJson = next)}
                />
              </motion.div>

              <!-- Action buttons sit directly under the (auto-scaled) code block -->
              <motion.div
                initial={{ opacity: 0, y: 30, scale: 0.95 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                transition={{ duration: 0.5, delay: 0.2, type: "spring", stiffness: 200 }}
              >
                <ActionBar
                  {isInvoking}
                  onInvoke={invoke}
                  onFillDefaults={fillDefaults}
                  onFormatJson={formatJson}
                  onClear={clearJson}
                />
                {#if statusMessage !== null}
                  <motion.div
                    class="shrink-0 px-3 pb-2 text-[12px] text-red"
                    initial={{ opacity: 0, x: -20 }}
                    animate={{
                      x: [0, 5, -5, 0],
                      opacity: [0.8, 1, 0.8]
                    }}
                    transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                  >
                    {statusMessage}
                  </motion.div>
                {/if}
              </motion.div>
            </motion.div>
          {/if}
        {:else if $activeTab === "response"}
          <motion.div 
            class="h-full"
            initial={{ opacity: 0, y: 30, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.5, type: "spring", stiffness: 200 }}
          >
            <ResponsePanel
              onInvoke={invoke}
              onSwitchToRequest={() => ($activeTab = "request")}
              {isInvoking}
            />
          </motion.div>
        {:else if $activeTab === "metadata"}
          <motion.div 
            class="h-full overflow-auto p-5"
            initial={{ opacity: 0, y: 30, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.5, type: "spring", stiffness: 200 }}
          >
            <MetadataTable />
          </motion.div>
        {:else if $activeTab === "proto"}
          <motion.div 
            class="h-full overflow-auto p-5"
            initial={{ opacity: 0, y: 30, scale: 0.95 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            transition={{ duration: 0.5, type: "spring", stiffness: 200 }}
          >
            <ProtoView {service} {method} />
          </motion.div>
        {/if}
      </motion.div>
    </motion.section>
  {/if}
{/if}