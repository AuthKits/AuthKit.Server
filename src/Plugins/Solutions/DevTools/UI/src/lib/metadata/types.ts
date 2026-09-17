export type MetadataEntry = [string, string];

/**
 * Fallback values used to guarantee the gRPC status columns always render,
 * even when the invocation result omits them (e.g. transport-level errors).
 */
export interface MetadataFallbacks {
  status?: string;
  statusName?: string;
  statusCode?: number;
  contentType?: string;
}