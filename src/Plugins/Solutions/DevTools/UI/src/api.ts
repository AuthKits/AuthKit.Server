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
  fetchServices(): Promise<GrpcCatalogResponse>;
  invoke(request: InvokeGrpcRequest): Promise<InvocationResult>;
}

export class HttpGrpcApi implements GrpcApi
{
  constructor(private readonly base: string) {}

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
