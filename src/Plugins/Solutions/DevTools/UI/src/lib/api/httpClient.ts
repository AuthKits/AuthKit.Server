/**
 * HTTP client for making API requests.
 * Handles fetch operations and base path resolution.
 */

export interface HttpClient {
  get(url: string): Promise<unknown>;
  post(url: string, body: unknown): Promise<unknown>;
}

export class BrowserHttpClient implements HttpClient {
  private readonly base: string;

  constructor(basePath: string = "/grpc-ui") {
    this.base = this.resolveBasePath(basePath);
  }

  async get(url: string): Promise<unknown> {
    const response = await fetch(`${this.base}${url}`);
    if (!response.ok) {
      throw new Error(`GET request failed: ${response.status} ${response.statusText}`);
    }
    return response.json();
  }

  async post(url: string, body: unknown): Promise<unknown> {
    const response = await fetch(`${this.base}${url}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!response.ok) {
      throw new Error(`POST request failed: ${response.status} ${response.statusText}`);
    }
    return response.json();
  }

  private resolveBasePath(defaultPath: string): string {
    if (typeof window === "undefined") return defaultPath;
    const injected = (window as { __GRPC_UI_BASE__?: string }).__GRPC_UI_BASE__;
    return injected !== undefined && !injected.includes("{{") && injected !== "" 
      ? injected 
      : defaultPath;
  }
}