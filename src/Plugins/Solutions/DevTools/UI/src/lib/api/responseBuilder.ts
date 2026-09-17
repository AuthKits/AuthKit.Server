/**
 * Response builder for creating InvokeResult objects.
 * Handles construction of success and error responses from wire format.
 */

import type { InvokeResult } from "../types";
import type { WireInvocationResult } from "./normalizer";

export interface ResponseBuilder {
  buildSuccess(wire: WireInvocationResult, durationMs: number): InvokeResult;
  buildError(wire: WireInvocationResult, durationMs: number): InvokeResult;
}

export class DefaultResponseBuilder implements ResponseBuilder {
  buildSuccess(wire: WireInvocationResult, durationMs: number): InvokeResult {
    return {
      status: "OK",
      statusName: wire.StatusName,
      statusCode: wire.StatusCode,
      durationMs,
      contentType: "application/grpc",
      body: wire.ResponseJson ? JSON.parse(wire.ResponseJson) : null,
      rawBase64: wire.ResponseBase64,
      trailers: {
        "grpc-status": String(wire.StatusCode),
        "grpc-message": "",
        "content-type": "application/grpc",
        ...(wire.Trailers ?? {}),
      },
      metadata: wire.Trailers ?? {},
    };
  }

  buildError(wire: WireInvocationResult, durationMs: number): InvokeResult {
    return {
      status: "ERROR",
      statusName: wire.StatusName,
      statusCode: wire.StatusCode,
      durationMs,
      contentType: "application/grpc",
      body: { error: wire.Detail ?? wire.StatusName ?? "Unknown gRPC error." },
      rawBase64: wire.ResponseBase64,
      trailers: {
        "grpc-status": String(wire.StatusCode),
        "grpc-message": wire.Detail ?? wire.StatusName,
        "content-type": "application/grpc",
        ...(wire.Trailers ?? {}),
      },
      metadata: wire.Trailers ?? {},
    };
  }
}