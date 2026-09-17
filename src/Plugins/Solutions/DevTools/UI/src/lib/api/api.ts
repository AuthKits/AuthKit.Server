/**
 * gRPC API client for DevTools UI.
 * Provides HTTP-based API calls via DevTools middleware.
 * Refactored following SOLID principles.
 */

import type { GrpcApi, GrpcMethod, GrpcService, InvokeRequest, InvokeResult } from "../types";
import { formatError } from "../format/format";
import { BrowserHttpClient, type HttpClient } from "./httpClient";
import {
  normalizeService,
  normalizeResult,
  type WireInvocationResult
} from "./normalizer";
import { mapService } from "./mapper";
import { DefaultResponseBuilder, type ResponseBuilder } from "./responseBuilder";
import { DefaultRequestBuilder, type RequestBuilder } from "./requestBuilder";

/** Public API. */

/** HTTP based gRPC API client implemented via DevTools middleware. */
export class HttpGrpcApi implements GrpcApi {
  private readonly httpClient: HttpClient;
  private readonly responseBuilder: ResponseBuilder;
  private readonly requestBuilder: RequestBuilder;

  constructor(
    httpClient?: HttpClient,
    responseBuilder?: ResponseBuilder,
    requestBuilder?: RequestBuilder
  ) {
    this.httpClient = httpClient ?? new BrowserHttpClient();
    this.responseBuilder = responseBuilder ?? new DefaultResponseBuilder();
    this.requestBuilder = requestBuilder ?? new DefaultRequestBuilder();
  }

  async fetchServices(): Promise<readonly GrpcService[]> {
    const raw = await this.httpClient.get("/api/services") as Record<string, unknown>;
    const target = this.pickString(raw, "Target", "target");
    const servicesRaw = this.pick<unknown[]>(raw, "Services", "services");
    return Array.isArray(servicesRaw)
      ? servicesRaw.map((s) => mapService(normalizeService(s), target ?? "unknown"))
      : [];
  }

  async invoke(request: InvokeRequest): Promise<InvokeResult> {
    const started = performance.now();
    try {
      const raw = await this.httpClient.post("/api/invoke", {
        Service: request.service,
        Method: request.method,
        RequestJson: request.requestJson,
        Headers: request.headers,
      }) as Record<string, unknown>;

      const result = normalizeResult(raw);
      const durationMs = Math.round(result.ElapsedMs ?? performance.now() - started);
      return result.Success
        ? this.responseBuilder.buildSuccess(result, durationMs)
        : this.responseBuilder.buildError(result, durationMs);
    } catch (err: unknown) {
      return this.buildErrorResult(err, started);
    }
  }

  private buildErrorResult(err: unknown, started: number): InvokeResult {
    return {
      status: "ERROR",
      statusName: "Unavailable",
      statusCode: 14,
      durationMs: Math.round(performance.now() - started),
      contentType: "application/grpc",
      body: { error: formatError(err) },
      trailers: {
        "grpc-status": "14",
        "grpc-message": formatError(err),
        "content-type": "application/grpc",
      },
      metadata: {},
    };
  }

  private pick<T>(value: Record<string, unknown>, ...keys: string[]): T | undefined {
    for (const key of keys) {
      const found = value[key];
      if (found !== undefined) return found as T;
    }
    return undefined;
  }

  private pickString(value: Record<string, unknown>, ...keys: string[]): string | undefined {
    const result = this.pick(value, ...keys);
    return result !== undefined && result !== null ? String(result) : undefined;
  }
}
/** Request builders. */
 
const defaultRequestBuilder = new DefaultRequestBuilder();

/** Default request JSON with recursion guard. */
export function defaultRequestJson(method: GrpcMethod): string {
  return defaultRequestBuilder.buildDefaultRequestJson(method);
}

export function defaultResponseJson(method: GrpcMethod): Record<string, unknown> {
  return defaultRequestBuilder.buildDefaultResponseJson(method);
}
