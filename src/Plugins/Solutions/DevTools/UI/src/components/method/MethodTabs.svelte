<script lang="ts">
  import { activeTab } from "../../lib/stores";

  interface TabItem {
    id: "request" | "response" | "metadata" | "proto";
    label: string;
  }

  const tabs: readonly TabItem[] = [
    { id: "request", label: "Request" },
    { id: "response", label: "Response" },
    { id: "metadata", label: "Metadata" },
    { id: "proto", label: "Proto" },
  ];
</script>

<div class="flex shrink-0 items-center gap-1 border-b border-border bg-panel2/40 px-3">
  {#each tabs as tab (tab.id)}
    {@const active = $activeTab === tab.id}
    <button
      type="button"
      class="flex cursor-pointer items-center gap-1.5 border-0 border-b-2 bg-transparent px-3.5 py-2.5 text-[13px] font-medium transition-colors {active
        ? 'border-accent text-text'
        : 'border-transparent text-muted hover:text-text'}"
      onclick={() => ($activeTab = tab.id)}
    >
      <span>{tab.label}</span>
    </button>
  {/each}
</div>