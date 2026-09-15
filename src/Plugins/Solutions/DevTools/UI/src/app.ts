import { HttpGrpcApi, type GrpcApi } from "./api";
import { createInvokeAction } from "./controller";
import { el, getById } from "./dom";
import { highlightSelectedMethod, renderMethodDetail, renderServiceList } from "./render";
import { errorMessage } from "./schema";
import { InMemoryDevToolsStore, type DevToolsStore } from "./store";
import { loadPersistedHeaders } from "./storage";
import type { GrpcCatalogResponse } from "./types";

declare global {
  interface Window {
    __GRPC_UI_BASE__?: string;
  }
}

const base = window.__GRPC_UI_BASE__ ?? "";
const api: GrpcApi = new HttpGrpcApi(base);

function bootstrap(api: GrpcApi, store: DevToolsStore): void
{
  const servicesContainer = getById("services");
  const detailContainer = getById("detail");
  const target = getById<HTMLSpanElement>("target");

  function onSelect(serviceIndex: number, methodIndex: number): void
  {
    store.select(serviceIndex, methodIndex);
    highlightSelectedMethod(store.state.selected);

    const service = store.state.services[serviceIndex];
    const method = service.methods[methodIndex];
    renderMethodDetail(
      detailContainer,
      method,
      loadPersistedHeaders(),
      createInvokeAction(api, service, method),
    );
  }

  async function onCatalog(catalog: GrpcCatalogResponse): Promise<void>
  {
    store.setServices(catalog.services ?? []);
    target.textContent = catalog.target ?? "";
    renderServiceList(servicesContainer, store.state.services, store.state.selected, onSelect);
  }

  void (async () => {
    try {
      await onCatalog(await api.fetchServices());
    } catch (err: unknown) {
      servicesContainer.replaceChildren();
      const message = el("div", "empty", `Failed to load catalog: ${errorMessage(err)}`);
      message.style.padding = "14px";
      servicesContainer.append(message);
    }
  })();
}

bootstrap(api, new InMemoryDevToolsStore());