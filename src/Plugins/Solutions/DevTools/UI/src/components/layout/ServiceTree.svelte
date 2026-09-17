<script lang="ts">
  import {
    selectedMethodName,
    selectedServiceName,
    services,
  } from "../../lib/stores";
  import {
    expandAllServices,
    filterServices,
    hostServices,
    pluginServices,
    serviceId,
  } from "../../lib/navigation/navigation";
  import type { GrpcService } from "../../lib/types";

  let { search }: { search: string } = $props();

  let expanded = $state<Record<string, boolean>>({});
  let isHostOpen = $state(true);
  let isPluginsOpen = $state(true);

  const inService = $derived(filterServices($services, search));

  const host = $derived(hostServices(inService));

  const plugin = $derived(pluginServices(inService));

  const inServiceSearch = $derived(search.trim() !== "");

  function toggleService(service: GrpcService): void {
    const id = serviceId(service);
    expanded[id] = !expanded[id];
    $selectedServiceName = id;
    if (service.methods.length > 0) {
      $selectedMethodName = service.methods[0].name;
    }
  }

  function selectMethod(service: GrpcService, methodName: string): void {
    const id = serviceId(service);
    expanded[id] = true;
    $selectedServiceName = id;
    $selectedMethodName = methodName;
  }

  $effect(() => {
    if (inServiceSearch) {
      isHostOpen = true;
      isPluginsOpen = true;
      expanded = expandAllServices($services);
    }
  });
</script>

{#if inService.length === 0}
  <div class="px-3 py-6 text-center text-[12px] text-muted">
    No services match “{search}”.
  </div>
{:else}
  {#if host.length > 0}
    <div class="mb-3">
      <button
        type="button"
        class="mb-1 flex w-full cursor-pointer items-center justify-between rounded-md px-2 py-1.5 text-left transition-colors hover:bg-panel2/60"
        onclick={() => (isHostOpen = !isHostOpen)}
        aria-expanded={isHostOpen}
      >
        <div class="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-muted">
          <svg
            width="12"
            height="12"
            viewBox="0 0 16 16"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            class="shrink-0 transition-transform duration-150"
            class:rotate-90={isHostOpen}
            class:rotate-0={!isHostOpen}
          >
            <path d="m6 4 4 4-4 4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <svg
            width="13"
            height="13"
            viewBox="0 0 16 16"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            class="shrink-0 text-muted"
          >
            <rect x="2" y="3" width="12" height="4" rx="1" />
            <rect x="2" y="9" width="12" height="4" rx="1" />
            <circle cx="4.5" cy="5" r="0.75" fill="currentColor" />
            <circle cx="4.5" cy="11" r="0.75" fill="currentColor" />
          </svg>
          <span>Host</span>
        </div>
        <span class="rounded bg-panel2 px-1.5 py-0.5 font-mono text-[10px] text-muted">
          {host.length}
        </span>
      </button>

      {#if isHostOpen}
        {#each host as service (serviceId(service))}
          {@const id = serviceId(service)}
          {@const open = !!expanded[id]}
          {@const active = $selectedServiceName === id}

          <div class="service-group mb-0.5">
            <div
              class="service-group-row flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-1.5 text-[13px] hover:bg-panel2/80 transition-colors"
              class:bg-panel2={active}
              onclick={() => toggleService(service)}
              role="button"
              tabindex="0"
              onkeydown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  toggleService(service);
                }
              }}
            >
              <svg
                width="12"
                height="12"
                viewBox="0 0 16 16"
                fill="none"
                stroke="var(--color-muted)"
                stroke-width="1.5"
                class="shrink-0 transition-transform duration-150"
                class:rotate-90={open}
                class:rotate-0={!open}
              >
                <path d="m6 4 4 4-4 4" stroke-linecap="round" stroke-linejoin="round" />
              </svg>

              <span
                class="flex size-5 shrink-0 items-center justify-center rounded-md font-mono text-[10px] font-bold bg-accent/15 text-accent"
              >
                <svg
                  width="12"
                  height="12"
                  viewBox="0 0 16 16"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.5"
                >
                  <rect x="2" y="2" width="12" height="12" rx="3" />
                  <path d="m6 6 4 4M6 10 10 6" stroke-linecap="round" />
                </svg>
              </span>

              <span
                class="min-w-0 flex-1 truncate text-[12.5px]"
                class:font-semibold={active}
              >
                {service.package}.{service.name}
              </span>
              <span class="shrink-0 font-mono text-[10px] text-muted">
                ({service.methods.length})
              </span>
            </div>

            {#if open}
              <ul class="method-list m-0 list-none p-0">
                {#each service.methods as method (method.name)}
                  {@const selected =
                    $selectedServiceName === id &&
                    $selectedMethodName === method.name}
                  <li>
                    <button
                      type="button"
                      class="flex w-full cursor-pointer items-center gap-2 rounded-lg border-0 py-1.5 pl-9 pr-2.5 text-left text-[12.5px] font-mono transition-colors {selected
                        ? 'bg-accent text-white font-medium'
                        : 'text-muted hover:bg-panel2 hover:text-text'}"

                      onclick={() => selectMethod(service, method.name)}
                    >
                      <span
                        class="shrink-0 rounded px-1 py-0.5 font-mono text-[9px] font-bold uppercase tracking-wide {selected
                          ? 'bg-white text-accent'
                          : 'text-accent'}"
                      >
                        rpc
                      </span>
                      <span class="min-w-0 flex-1 truncate">{method.name}</span>
                    </button>
                  </li>
                {/each}
              </ul>
            {/if}
          </div>
        {/each}
      {/if}
    </div>
  {/if}

  {#if plugin.length > 0}
    <div class="mb-3">
      <button
        type="button"
        class="mb-1 flex w-full cursor-pointer items-center justify-between rounded-md px-2 py-1.5 text-left transition-colors hover:bg-panel2/60"
        onclick={() => (isPluginsOpen = !isPluginsOpen)}
        aria-expanded={isPluginsOpen}
      >
        <div class="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-muted">
          <svg
            width="12"
            height="12"
            viewBox="0 0 16 16"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            class="shrink-0 transition-transform duration-150"
            class:rotate-90={isPluginsOpen}
            class:rotate-0={!isPluginsOpen}
          >
            <path d="m6 4 4 4-4 4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <svg
            width="13"
            height="13"
            viewBox="0 0 16 16"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            class="shrink-0 text-muted"
          >
            <path
              d="M6 2v1.5a1.5 1.5 0 0 0 3 0V2h3a1 1 0 0 1 1 1v3h-1.5a1.5 1.5 0 0 0 0 3H13v3a1 1 0 0 1-1 1H9v-1.5a1.5 1.5 0 0 0-3 0V14H3a1 1 0 0 1-1-1v-3h1.5a1.5 1.5 0 0 0 0-3H2V3a1 1 0 0 1 1-1h3z"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
          <span>Plugins</span>
        </div>
        <span class="rounded bg-panel2 px-1.5 py-0.5 font-mono text-[10px] text-muted">
          {plugin.length}
        </span>
      </button>

      {#if isPluginsOpen}
        {#each plugin as service (serviceId(service))}
          {@const id = serviceId(service)}
          {@const open = !!expanded[id]}
          {@const active = $selectedServiceName === id}

          <div class="service-group mb-0.5">
            <div
              class="service-group-row flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-1.5 text-[13px] hover:bg-panel2/80 transition-colors"
              class:bg-panel2={active}
              onclick={() => toggleService(service)}
              role="button"
              tabindex="0"
              onkeydown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  toggleService(service);
                }
              }}
            >
              <svg
                width="12"
                height="12"
                viewBox="0 0 16 16"
                fill="none"
                stroke="var(--color-muted)"
                stroke-width="1.5"
                class="shrink-0 transition-transform duration-150"
                class:rotate-90={open}
                class:rotate-0={!open}
              >
                <path d="m6 4 4 4-4 4" stroke-linecap="round" stroke-linejoin="round" />
              </svg>

              <span
                class="flex size-5 shrink-0 items-center justify-center rounded-md font-mono text-[10px] font-bold bg-accent2/15 text-accent2"
              >
                <svg
                  width="12"
                  height="12"
                  viewBox="0 0 16 16"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="1.5"
                >
                  <path
                    d="M6 2v1.5a1.5 1.5 0 0 0 3 0V2h3a1 1 0 0 1 1 1v3h-1.5a1.5 1.5 0 0 0 0 3H13v3a1 1 0 0 1-1 1H9v-1.5a1.5 1.5 0 0 0-3 0V14H3a1 1 0 0 1-1-1v-3h1.5a1.5 1.5 0 0 0 0-3H2V3a1 1 0 0 1 1-1h3z"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  />
                </svg>
              </span>

              <span
                class="min-w-0 flex-1 truncate text-[12.5px]"
                class:font-semibold={active}
              >
                {service.package}.{service.name}
              </span>
              <span class="shrink-0 font-mono text-[10px] text-muted">
                ({service.methods.length})
              </span>
            </div>

            {#if open}
              <ul class="method-list m-0 list-none p-0">
                {#each service.methods as method (method.name)}
                  {@const selected =
                    $selectedServiceName === id &&
                    $selectedMethodName === method.name}
                  <li>
                    <button
                      type="button"
                      class="flex w-full cursor-pointer items-center gap-2 rounded-lg border-0 py-1.5 pl-9 pr-2.5 text-left text-[12.5px] font-mono transition-colors {selected
                        ? 'bg-accent text-white font-medium'
                        : 'text-muted hover:bg-panel2 hover:text-text'}"

                      onclick={() => selectMethod(service, method.name)}
                    >
                      <span
                        class="shrink-0 rounded px-1 py-0.5 font-mono text-[9px] font-bold uppercase tracking-wide {selected
                          ? 'bg-white text-accent'
                          : 'text-accent'}"
                      >
                        rpc
                      </span>
                      <span class="min-w-0 flex-1 truncate">{method.name}</span>
                    </button>
                  </li>
                {/each}
              </ul>
            {/if}
          </div>
        {/each}
      {/if}
    </div>
  {/if}
{/if}
