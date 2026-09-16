import type { GrpcApi, InvokeGrpcRequest } from "./api";
import {
  errorMessage,
  formatInvocationOutput,
  parseHeaders,
} from "./schema";
import { persistHeaders } from "./storage";
import type { MethodDetail } from "./render";
import type { GrpcMethodInfo, GrpcServiceInfo } from "./types";

/** Creates the UI action that invokes the selected gRPC method. */
export function createInvokeAction(
  api: GrpcApi,
  service: GrpcServiceInfo,
  method: GrpcMethodInfo,
): (detail: MethodDetail) => void
{
  return (detail) =>
  {
    void invoke(api, service, method, detail);
  };
}

/** Executes an invocation and writes its progress and result to the detail view. */
async function invoke(
  api: GrpcApi,
  service: GrpcServiceInfo,
  method: GrpcMethodInfo,
  detail: MethodDetail,
): Promise<void>
{
  const headersText = detail.headers.value;
  persistHeaders(headersText);

  const request: InvokeGrpcRequest = {
    service: service.fullName || `${service.package}.${service.name}`,
    method: method.name,
    requestJson: detail.request.value,
    headers: parseHeaders(headersText),
  };

  detail.setStatus("idle", "Invoking…");
  detail.writeResponse("");

  try
  {
    const result = await api.invoke(request);
    const success = result.success && result.statusCode === 0;

    detail.setStatus(
      success ? "ok" : "err",
      `${result.statusName} · ${result.elapsedMs} ms`,
    );
    detail.writeResponse(formatInvocationOutput(result));
  } catch (err: unknown) {
    detail.setStatus("err", "Request failed");
    detail.writeResponse(errorMessage(err));
  }
}
