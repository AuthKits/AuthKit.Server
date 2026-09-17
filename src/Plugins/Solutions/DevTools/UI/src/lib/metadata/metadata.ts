import { parseHeaders } from "../format/format";
import type { MetadataEntry, MetadataFallbacks } from "./types";

/**
 * gRPC standard keys surfaced first so the row order is stable regardless of
 * the wire shape.
 */
const PREFERRED_ORDER = [
  "content-type",
  "grpc-encoding",
  "grpc-status",
  "grpc-message",
] as const;

/**
 * Merge response `metadata` and `trailers` into a single ordered list. gRPC
 * standard keys are surfaced first (with safe fallbacks); no values are
 * hardcoded except the fallbacks passed by the caller.
 */
export function parseMetadata(
  metadata: Record<string, string> | undefined,
  trailers: Record<string, string> | undefined,
  fallbacks: MetadataFallbacks = {},
): MetadataEntry[] {
  const received: Record<string, string> = {
    ...(metadata ?? {}),
    ...(trailers ?? {}),
  };

  if (received["grpc-status"] === undefined) {
    received["grpc-status"] = String(fallbacks.statusCode ?? (fallbacks.status === "OK" ? 0 : 1));
  }
  if (received["grpc-message"] === undefined) {
    received["grpc-message"] = fallbacks.statusName ?? "";
  }
  if (received["content-type"] === undefined) {
    received["content-type"] = fallbacks.contentType ?? "application/grpc";
  }

  const ordered: MetadataEntry[] = [];
  const seen = new Set<string>();
  for (const key of PREFERRED_ORDER) {
    if (received[key] !== undefined) {
      ordered.push([key, received[key]]);
      seen.add(key);
    }
  }
  for (const [key, value] of Object.entries(received)) {
    if (!seen.has(key)) {
      ordered.push([key, value]);
      seen.add(key);
    }
  }
  return ordered;
}

/**
 * Tap the typed request headers (in `Key: Value` form, one per line) into a
 * metadata entry list.
 */
export function parseRequestMetadata(value: string): MetadataEntry[] {
  return Object.entries(parseHeaders(value));
}

export function formatMetadata(entries: MetadataEntry[]): string {
  return entries.map(([key, value]) => `${key}: ${value}`).join("\n");
}