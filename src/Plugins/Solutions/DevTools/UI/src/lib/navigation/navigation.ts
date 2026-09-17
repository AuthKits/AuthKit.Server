import type { GrpcService } from "../types";

export function serviceId(service: GrpcService): string {
  return service.package + "." + service.name;
}

export function matchesSearch(service: GrpcService, search: string): boolean {
  const query = search.trim();
  if (query === "") return true;
  return serviceId(service).toLowerCase().includes(query.toLowerCase());
}

export function filterServices(
  services: readonly GrpcService[],
  search: string,
): GrpcService[] {
  return services.filter((service) => matchesSearch(service, search));
}

export function hostServices(services: readonly GrpcService[]): GrpcService[] {
  return services.filter((service) => !service.isPlugin);
}

export function pluginServices(
  services: readonly GrpcService[],
): GrpcService[] {
  return services.filter((service) => !!service.isPlugin);
}

/** Expand every service (used when search query is active). */
export function expandAllServices(
  services: readonly GrpcService[],
): Record<string, boolean> {
  const expanded: Record<string, boolean> = {};
  for (const service of services) {
    expanded[serviceId(service)] = true;
  }
  return expanded;
}

/** True for the global "focus the service search box" shortcut (⌘K / Ctrl+K). */
export function isSearchShortcut(event: KeyboardEvent): boolean {
  return (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k";
}