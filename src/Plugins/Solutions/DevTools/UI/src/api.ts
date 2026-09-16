import type { GrpcCatalogResponse, InvocationResult } from "./types";

export interface InvokeGrpcRequest
{
  service: string;
  method: string;
  requestJson: string;
  headers: Record<string, string>;
}

export interface GrpcApi
{
  /** Loads the services and methods exposed by the DevTools catalog. */
  fetchServices(): Promise<GrpcCatalogResponse>;

  /** Invokes a gRPC method through the DevTools HTTP endpoint. */
  invoke(request: InvokeGrpcRequest): Promise<InvocationResult>;
}

export class HttpGrpcApi implements GrpcApi
{
  /** Creates an HTTP API client rooted at the supplied DevTools path. */
  constructor(private readonly base: string) {}

  /** Loads the services and methods exposed by the DevTools catalog. */
  async fetchServices(): Promise<GrpcCatalogResponse>
  {
    const response = await fetch(`${this.base}/api/services`);

    if (!response.ok) {
      throw new Error(
        `Failed to load catalog: HTTP ${response.status}`,
      );
    }

    return (await response.json()) as GrpcCatalogResponse;
  }

  /** Invokes a gRPC method through the DevTools HTTP endpoint. */
  async invoke(request: InvokeGrpcRequest): Promise<InvocationResult>
  {
    const response = await fetch(`${this.base}/api/invoke`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error(
        `Failed to invoke gRPC method: HTTP ${response.status}`,
      );
    }

    return (await response.json()) as InvocationResult;
  }
}
