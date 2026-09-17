import type { GrpcMethod, GrpcService, ProtoField } from "../types";
import { highlightProto, type CodeLine } from "../format/jsonHighlight";

const TYPE_MAP: Record<string, string> = {
  string: "string",
  int32: "int32",
  int64: "int64",
  uint32: "uint32",
  uint64: "uint64",
  sint32: "sint32",
  sint64: "sint64",
  fixed32: "fixed32",
  fixed64: "fixed64",
  sfixed32: "sfixed32",
  sfixed64: "sfixed64",
  bool: "bool",
  boolean: "bool",
  float: "float",
  double: "double",
  bytes: "bytes",
};

export function formatFieldType(field: ProtoField): string {
  if (field.message) return field.message.name || "Message";
  const low = field.fieldType.toLowerCase();
  return (
    TYPE_MAP[low] ??
    (field.fieldType.startsWith("google.protobuf.")
      ? field.fieldType.split(".").pop() ?? field.fieldType
      : field.fieldType.toLowerCase())
  );
}

function renderFieldLines(fields?: ProtoField[]): string {
  if (!fields || fields.length === 0) return "";
  return fields
    .map((f) => {
      let typeName = formatFieldType(f);
      if (f.repeated) typeName = `repeated ${typeName}`;
      if (f.map) {
        typeName = `map<${f.mapKeyType ?? "string"}, ${f.mapValueType ?? typeName}>`;
      }
      return `  ${typeName} ${f.name} = ${f.number};`;
    })
    .join("\n");
}

export function requestProtoCode(service: GrpcService, method: GrpcMethod): string {
  const pkg = service.package || "authkit.example";
  const svcName = service.name || "ExampleHello";
  const reqType = method.requestType.split(".").pop() || "Request";
  const resType = method.responseType.split(".").pop() || "Response";
  const fields = renderFieldLines(method.requestFields);
  const body = fields ? `\n${fields}\n` : "\n";
  return `syntax = "proto3";\n\npackage ${pkg};\n\nmessage ${reqType} {${body}}\n\nservice ${svcName} {\n  rpc ${method.name}(${reqType}) returns (${resType});\n}`;
}

export function responseProtoCode(service: GrpcService, method: GrpcMethod): string {
  const pkg = service.package || "authkit.example";
  const resType = method.responseType.split(".").pop() || "Response";
  const fields = renderFieldLines(method.responseFields);
  const body = fields ? `\n${fields}\n` : "\n";
  return `syntax = "proto3";\n\npackage ${pkg};\n\nmessage ${resType} {${body}}`;
}

export function parseJsonSafe(text: string): unknown {
  try {
    if (text && text.trim() !== "") {
      return JSON.parse(text);
    }
  } catch {
    // fall back to {}
  }
  return {};
}

export function protoCodeLines(proto: string): CodeLine[] {
  return proto.split("\n").map((line, index) => ({
    number: index + 1,
    html: highlightProto(line),
    raw: line,
  }));
}