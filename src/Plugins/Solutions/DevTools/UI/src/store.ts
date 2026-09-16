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
  /** Replaces the current service catalog. */
  setServices(services: readonly GrpcServiceInfo[]): void;
  /** Records the selected service and method indexes. */
  select(serviceIndex: number, methodIndex: number): void;
}

export class InMemoryDevToolsStore implements DevToolsStore
{
  private current: DevToolsState = { services: [], selected: null };

  /** Returns the current immutable state snapshot. */
  get state(): DevToolsState {
    return this.current;
  }

  /** Replaces the current service catalog. */
  setServices(services: readonly GrpcServiceInfo[]): void {
    this.current = { ...this.current, services };
  }

  /** Records the selected service and method indexes. */
  select(serviceIndex: number, methodIndex: number): void {
    this.current = { ...this.current, selected: [serviceIndex, methodIndex] as const };
  }
}
