import { derived, writable } from "svelte/store";
import { HttpGrpcApi } from "./api/api";
import type { GrpcApi, GrpcService, InvokeResult } from "./types";

export const connectionStatus = writable<"connecting" | "connected" | "error">(
  "connecting",
);

const isDev = import.meta.env.DEV === true;

export const api: GrpcApi = isDev ? new HttpGrpcApi() : new HttpGrpcApi();

export const services = writable<readonly GrpcService[]>([]);
export const selectedServiceName = writable<string | null>(null);
export const selectedMethodName = writable<string | null>(null);
export const requestJson = writable<string>("");
export const response = writable<InvokeResult | null>(null);
export const connectedTarget = writable("http://localhost:5000");
export const activeTab = writable<"request" | "response" | "metadata" | "proto">(
  "request",
);

/**
 * The raw gRPC metadata (request headers) typed by the user, in `Key: Value`
 * form (one per line). Persisted to `sessionStorage` under
 * `grpc-ui-headers` so it survives method switches within the same session.
 */
const REQUEST_HEADERS_STORAGE_KEY = "grpc-ui-headers";
const DEFAULT_REQUEST_HEADERS = "authorization: Bearer ";

function loadRequestHeaders(): string {
  if (typeof window === "undefined") return DEFAULT_REQUEST_HEADERS;
  return sessionStorage.getItem(REQUEST_HEADERS_STORAGE_KEY) ?? DEFAULT_REQUEST_HEADERS;
}

export const requestHeaders = writable<string>(loadRequestHeaders());
requestHeaders.subscribe((value) => {
  if (typeof window !== "undefined") {
    sessionStorage.setItem(REQUEST_HEADERS_STORAGE_KEY, value);
  }
});

export const selectedService = derived(
  [services, selectedServiceName],
  ([all, name]) => all.find((service) => service.package + "." + service.name === name) ?? null,
);

export type SelectedMethod = {
  service: string;
  methodName: string;
} | null;

export const selectedMethod = derived(
  [selectedService, selectedMethodName],
  ([service, methodName]) =>
    service === null || methodName === null
      ? null
      : { service: service.package + "." + service.name, methodName },
);

export const theme = writable<"dark" | "light">("dark");

// Persist theme preference to localStorage
theme.subscribe((value) => {
  if (typeof window !== "undefined") {
    localStorage.setItem("grpc-ui-theme", value);
  }
});

// Load theme from localStorage on init
if (typeof window !== "undefined") {
  const saved = localStorage.getItem("grpc-ui-theme");
  if (saved === "light" || saved === "dark") {
    theme.set(saved);
  }
}