import type {
  GrpcFieldSchema,
  GrpcMessageSchema,
  GrpcMethodInfo,
  InvocationResult,
} from "./types";

export type StreamingKind = "uni" | "cli" | "srv" | "bidi";

export function streamingKind(method: GrpcMethodInfo): StreamingKind
{
  if (method.isClientStreaming && method.isServerStreaming) return "bidi";
  if (method.isClientStreaming) return "cli";
  if (method.isServerStreaming) return "srv";
  return "uni";
}

export function describeStreaming(method: GrpcMethodInfo): string
{
  if (method.isClientStreaming && method.isServerStreaming) return "Bidirectional stream";
  if (method.isClientStreaming) return "Client streaming";
  if (method.isServerStreaming) return "Server streaming";
  return "Unary";
}

export function fieldTypeText(field: GrpcFieldSchema): string
{
  if (field.isMap)
  {
    const value = field.mapValue?.fullName ?? field.mapValueType ?? "?";
    return `map<${field.mapKeyType ?? "?"}, ${value}>`;
  }
  if (field.message && field.message.fullName !== field.message.name) return field.message.fullName;
  return field.fieldType;
}

export function jsonFromSchema(msg: GrpcMessageSchema, depth = 0): Record<string, unknown>
{
  const out: Record<string, unknown> = {};
  const seen = depth > 4;
  for (const field of msg.fields ?? [])
  {
    if (seen) { out[field.name] = null; continue; }
    if (field.isMap) { out[field.name] = {}; continue; }
    if (field.isRepeated) { out[field.name] = []; continue; }
    if (field.message) { out[field.name] = jsonFromSchema(field.message, depth + 1); continue; }
    if (field.fieldType === "Enum") { out[field.name] = field.enumValues?.[0] ?? "UNKNOWN"; continue; }
    switch (field.fieldType)
    {
      case "String": case "Bytes": out[field.name] = ""; break;
      case "Bool": out[field.name] = false; break;
      case "Double": case "Float": out[field.name] = 0.0; break;
      case "Int32": case "Int64": case "SInt32": case "SInt64":
      case "UInt32": case "UInt64": case "Fixed32": case "Fixed64":
      case "SFixed32": case "SFixed64": out[field.name] = 0; break;
      default: out[field.name] = null;
    }
  }
  return out;
}

export function parseHeaders(text: string): Record<string, string>
{
  const headers: Record<string, string> = {};
  for (const line of text.split("\n"))
  {
    const separator = line.indexOf(":");
    if (separator <= 0) continue;
    const key = line.slice(0, separator).trim();
    const value = line.slice(separator + 1).trim();
    if (key) headers[key] = value;
  }
  return headers;
}

export function prettyPrint(text: unknown): string
{
  if (typeof text === "string")
  {
    try {
      return JSON.stringify(JSON.parse(text), null, 2);
    }
    catch { return text; }
  }
  if (text && typeof text === "object") return JSON.stringify(text, null, 2);
  return String(text ?? "");
}

export function formatInvocationOutput(body: InvocationResult): string
{
  const parts: string[] = [];
  if (body.detail) parts.push(`Error: ${body.detail}`);
  
  if (body.responseJson)
  {
    try { parts.push(JSON.stringify(JSON.parse(body.responseJson), null, 2)); }
    catch { parts.push(body.responseJson); }
  }
  
  if (body.trailers && Object.keys(body.trailers).length > 0)
  {
    parts.push("Trailers:\n" + Object.entries(body.trailers)
      .map(([key, value]) => `${key}: ${value}`).join("\n"));
  }
  
  return parts.length > 0 ? parts.join("\n\n") : "(no response body)";
}

export function errorMessage(err: unknown): string
{
  return err instanceof Error ? err.message : String(err);
}
