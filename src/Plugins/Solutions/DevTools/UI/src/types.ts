/** Schema of protobuf scalar or composite field, mirroring GrpcFieldSchema. */
export interface GrpcFieldSchema {
  name: string;
  fieldType: string;
  isRepeated: boolean;
  isMap: boolean;
  mapKeyType?: string | null;
  mapValueType?: string | null;
  mapValue?: GrpcMessageSchema | null;
  message?: GrpcMessageSchema | null;
  enumType?: string | null;
  enumValues?: string[] | null;
}

/** Protobuf message layout, mirroring GrpcMessageSchema. */
export interface GrpcMessageSchema {
  name: string;
  fullName: string;
  fields: GrpcFieldSchema[];
}

/** Descriptor of single RPC method, mirroring GrpcMethodInfo. */
export interface GrpcMethodInfo {
  name: string;
  fullName: string;
  isClientStreaming: boolean;
  isServerStreaming: boolean;
  request: GrpcMessageSchema;
  response: GrpcMessageSchema;
}

/** Descriptor of gRPC service together with its methods, mirroring GrpcServiceInfo. */
export interface GrpcServiceInfo {
  name: string;
  fullName: string;
  package: string;
  fileName: string;
  methods: GrpcMethodInfo[];
}

/** Payload returned by the catalog API endpoint. */
export interface GrpcCatalogResponse {
  target: string;
  services: GrpcServiceInfo[];
}

/** Result of aRPC invocation, mirroring GrpcInvocationResult. */
export interface InvocationResult {
  success: boolean;
  statusName: string;
  statusCode: number;
  detail?: string | null;
  responseJson?: string | null;
  elapsedMs: number;
  trailers?: Record<string, string> | null;
}
