import type { GrpcServiceInfo } from "./types";

export type SelectedIndex = readonly [service: number, method: number];

export interface DevToolsState
{
  readonly services: readonly GrpcServiceInfo[];
  readonly selected: SelectedIndex | null;
}

export interface DevToolsStore
{
  readonly state: DevToolsState;
  setServices(services: readonly GrpcServiceInfo[]): void;
  select(serviceIndex: number, methodIndex: number): void;
}

export class InMemoryDevToolsStore implements DevToolsStore
{
  private current: DevToolsState = { services: [], selected: null };

  get state(): DevToolsState {
    return this.current;
  }

  setServices(services: readonly GrpcServiceInfo[]): void {
    this.current = { ...this.current, services };
  }

  select(serviceIndex: number, methodIndex: number): void {
    this.current = { ...this.current, selected: [serviceIndex, methodIndex] as const };
  }
}