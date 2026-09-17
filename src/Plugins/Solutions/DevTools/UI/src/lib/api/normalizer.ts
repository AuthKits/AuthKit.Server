/**
 * Normalizer for wire format data.
 * Converts various JSON field name formats to consistent wire format.
 */

export interface WireService {
  Name: string;
  FullName: string;
  Description?: string;
  Package: string;
  FileName: string;
  IsPlugin?: boolean;
  Methods: WireMethod[];
}

export interface WireMethod {
  Name: string;
  FullName: string;
  Description?: string;
  IsClientStreaming: boolean;
  IsServerStreaming: boolean;
  Request?: WireMessage;
  Response?: WireMessage;
}

export interface WireMessage {
  Name: string;
  FullName: string;
  Description?: string;
  Fields: WireField[];
}

export interface WireField {
  Name: string;
  Description?: string;
  FieldType: string;
  IsRepeated: boolean;
  IsMap: boolean;
  MapKeyType?: string;
  MapValueType?: string;
  EnumType?: string;
  EnumValues?: string[];
  Message?: WireMessage;
  MapValue?: WireMessage;
}

export interface WireInvocationResult {
  Success: boolean;
  StatusName: string;
  StatusCode: number;
  Detail?: string;
  ResponseJson?: string;
  ResponseBase64?: string;
  ElapsedMs: number;
  Trailers?: Record<string, string>;
}

function pick<T>(value: Record<string, unknown>, ...keys: string[]): T | undefined {
  for (const key of keys) {
    const found = value[key];
    if (found !== undefined) return found as T;
  }
  return undefined;
}

function optionalString(value: unknown): string | undefined {
  return value !== undefined && value !== null ? String(value) : undefined;
}

export function normalizeService(wire: unknown): WireService {
  const record = (wire ?? {}) as Record<string, unknown>;
  return {
    Name: String(pick(record, "Name", "name") ?? ""),
    FullName: String(pick(record, "FullName", "fullName") ?? ""),
    Description: optionalString(pick(record, "Description", "description")),
    Package: String(pick(record, "Package", "package") ?? ""),
    FileName: String(pick(record, "FileName", "fileName") ?? ""),
    IsPlugin: Boolean(pick(record, "IsPlugin", "isPlugin")),
    Methods: Array.isArray(pick(record, "Methods", "methods"))
      ? ((pick(record, "Methods", "methods") as unknown[]).map(normalizeMethod))
      : [],
  };
}

export function normalizeMethod(wire: unknown): WireMethod {
  const record = (wire ?? {}) as Record<string, unknown>;
  return {
    Name: String(pick(record, "Name", "name") ?? ""),
    FullName: String(pick(record, "FullName", "fullName") ?? ""),
    Description: optionalString(pick(record, "Description", "description")),
    IsClientStreaming: Boolean(pick(record, "IsClientStreaming", "isClientStreaming")),
    IsServerStreaming: Boolean(pick(record, "IsServerStreaming", "isServerStreaming")),
    Request: normalizeMessage(pick(record, "Request", "request")),
    Response: normalizeMessage(pick(record, "Response", "response")),
  };
}

export function normalizeMessage(wire: unknown): WireMessage {
  const record = (wire ?? {}) as Record<string, unknown>;
  return {
    Name: String(pick(record, "Name", "name") ?? ""),
    FullName: String(pick(record, "FullName", "fullName") ?? ""),
    Description: optionalString(pick(record, "Description", "description")),
    Fields: Array.isArray(pick(record, "Fields", "fields"))
      ? (pick(record, "Fields", "fields") as unknown[]).map(normalizeField)
      : [],
  };
}

export function normalizeField(wire: unknown): WireField {
  const record = (wire ?? {}) as Record<string, unknown>;
  return {
    Name: String(pick(record, "Name", "name") ?? ""),
    Description: optionalString(pick(record, "Description", "description")),
    FieldType: String(pick(record, "FieldType", "fieldType") ?? ""),
    IsRepeated: Boolean(pick(record, "IsRepeated", "isRepeated")),
    IsMap: Boolean(pick(record, "IsMap", "isMap")),
    MapKeyType: optionalString(pick(record, "MapKeyType", "mapKeyType")),
    MapValueType: optionalString(pick(record, "MapValueType", "mapValueType")),
    EnumType: optionalString(pick(record, "EnumType", "enumType")),
    EnumValues: Array.isArray(pick(record, "EnumValues", "enumValues"))
      ? (pick(record, "EnumValues", "enumValues") as unknown[]).map(String)
      : undefined,
    Message: pick(record, "Message", "message") != null ? normalizeMessage(pick(record, "Message", "message")) : undefined,
    MapValue: pick(record, "MapValue", "mapValue") != null ? normalizeMessage(pick(record, "MapValue", "mapValue")) : undefined,
  };
}

export function normalizeResult(wire: Record<string, unknown>): WireInvocationResult {
  return {
    Success: Boolean(pick(wire, "Success", "success")),
    StatusName: String(pick(wire, "StatusName", "statusName") ?? ""),
    StatusCode: Number(pick(wire, "StatusCode", "statusCode") ?? 0),
    Detail: optionalString(pick(wire, "Detail", "detail")),
    ResponseJson: optionalString(pick(wire, "ResponseJson", "responseJson")),
    ResponseBase64: optionalString(pick(wire, "ResponseBase64", "responseBase64")),
    ElapsedMs: Number(pick(wire, "ElapsedMs", "elapsedMs") ?? 0),
    Trailers: pick(wire, "Trailers", "trailers") as Record<string, string> | undefined,
  };
}