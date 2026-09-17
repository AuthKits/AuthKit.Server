export type JSONValue =
  | null
  | boolean
  | number
  | string
  | JSONValue[]
  | { [key: string]: JSONValue };

export interface ProtoField {
  name: string;
  description?: string;
  fieldType: string;
  repeated: boolean;
  map: boolean;
  mapKeyType?: string;
  mapValueType?: string;
  enumValues?: string[];
  message?: ProtoMessage;
}

export interface ProtoMessage {
  name: string;
  fullName: string;
  description?: string;
  fields: ProtoField[];
}

export interface GrpcMethod {
  name: string;
  fullName?: string;
  kind?: RpcKind;
  requestType: string;
  responseType: string;
  fullRequestPath: string;
  fullResponsePath: string;
  description?: string;
  requestFields?: ProtoField[];
  responseFields?: ProtoField[];
}

export interface GrpcService {
  name: string;
  package: string;
  description?: string;
  isPlugin?: boolean;
  methods: GrpcMethod[];
}

export type RpcKind = "unary" | "server streaming" | "client streaming" | "bidi streaming";

export type InvokeStatus = "OK" | "ERROR";

export interface InvokeResult {
  status: InvokeStatus;
  statusName?: string;
  statusCode?: number;
  durationMs: number;
  contentType: string;
  body: unknown;
  rawBase64?: string;
  trailers?: Record<string, string>;
  metadata: Record<string, string>;
}

export interface InvokeRequest {
  service: string;
  method: string;
  requestJson: string;
  headers: Record<string, string>;
}

export interface GrpcApi {
  fetchServices(): Promise<readonly GrpcService[]>;
  invoke(request: InvokeRequest): Promise<InvokeResult>;
}
