import { parseHeaders } from "../format/format";
import { defaultRequestJson } from "./api";
import type { GrpcMethod, GrpcService, InvokeRequest } from "../types";

export interface SelectedMethod {
  service: string;
  methodName: string;
}

export function buildInvokeRequest(
  method: SelectedMethod,
  requestJson: string,
  requestHeaders: string,
): InvokeRequest {
  return {
    service: method.service,
    method: method.methodName,
    requestJson,
    headers: parseHeaders(requestHeaders),
  };
}

export interface MethodReset {
  requestJson: string;
}

export function initialMethodState(
  service: GrpcService | null,
  methodName: string | null,
): MethodReset | null {
  if (service === null || methodName === null) return null;
  const method = service.methods.find((m) => m.name === methodName);
  if (method === undefined) return null;
  return { requestJson: defaultRequestJson(method) };
}