/**
 * Proto generator for creating .proto file content.
 * Handles generation of proto definitions from gRPC service metadata.
 */

import type { GrpcService, ProtoField, ProtoMessage } from "../types";

export interface ProtoGenerator {
  generatePreview(service: GrpcService): string;
}

export class DefaultProtoGenerator implements ProtoGenerator {
  private readonly SCALAR_MAP: Record<string, string> = {
    String: "string",
    Int32: "int32",
    Int64: "int64",
    UInt32: "uint32",
    UInt64: "uint64",
    SInt32: "sint32",
    SInt64: "sint64",
    Fixed32: "fixed32",
    Fixed64: "fixed64",
    SFixed32: "sfixed32",
    SFixed64: "sfixed64",
    Bool: "bool",
    Float: "float",
    Double: "double",
    Bytes: "bytes",
  };

  private readonly WELL_KNOWN_IMPORTS: Record<string, string> = {
    "google.protobuf.Empty": "google/protobuf/empty.proto",
    "google.protobuf.Timestamp": "google/protobuf/timestamp.proto",
    "google.protobuf.Duration": "google/protobuf/duration.proto",
    "google.protobuf.Any": "google/protobuf/any.proto",
    "google.protobuf.Value": "google/protobuf/struct.proto",
    "google.protobuf.Struct": "google/protobuf/struct.proto",
  };

  generatePreview(service: GrpcService): string {
    const imports = new Set<string>();
    const messages: ProtoMessage[] = [];
    const seen = new Set<string>();

    const visitMessage = (message: ProtoMessage | undefined, depth: number): void => {
      if (message === undefined) return;
      const fullName = message.fullName;
      if (this.WELL_KNOWN_IMPORTS[fullName] !== undefined) {
        imports.add(this.WELL_KNOWN_IMPORTS[fullName]);
        return;
      }
      if (fullName === "" || seen.has(fullName)) return;
      seen.add(fullName);
      messages.push(message);
      for (const field of message.fields) visitMessage(field.message, depth + 1);
    };

    for (const method of service.methods) {
      if (method.requestFields !== undefined) {
        visitMessage(
          { name: method.requestType, fullName: method.fullRequestPath, fields: method.requestFields },
          0
        );
      }
      if (method.responseFields !== undefined) {
        visitMessage(
          { name: method.responseType, fullName: method.fullResponsePath, fields: method.responseFields },
          0
        );
      }
    }

    const lines: string[] = [];
    lines.push('syntax = "proto3";');
    lines.push(`option csharp_namespace = "Host.Grpc";`);
    lines.push(`package ${service.package};`);
    if (imports.size > 0) {
      lines.push("");
      for (const importPath of [...imports].sort()) {
        lines.push(`import "${importPath}";`);
      }
    }
    lines.push("");
    if (service.description !== undefined && service.description !== "") {
      lines.push(`// ${service.description}`);
    }
    lines.push(`service ${service.name}\n{`);
    for (const method of service.methods) {
      const requestType = this.protoTypeName(method.fullRequestPath);
      const responseType = this.protoTypeName(method.fullResponsePath);
      if (method.description !== undefined && method.description !== "") {
        lines.push(`  // ${method.description}`);
      }
      lines.push(`  rpc ${method.name} (${requestType}) returns (${responseType});`);
    }
    lines.push("}");
    if (messages.length > 0) {
      lines.push("");
      for (const message of messages) {
        lines.push("");
        lines.push(this.renderMessage(message));
      }
    }
    return lines.join("\n");
  }

  private protoTypeName(fullName: string): string {
    return this.WELL_KNOWN_IMPORTS[fullName] !== undefined 
      ? fullName 
      : fullName.split(".").pop() ?? fullName;
  }

  private fieldProtoType(field: ProtoField): string {
    if (field.message !== undefined) return this.protoTypeName(field.message.fullName);
    if (field.fieldType.toLowerCase() === "enum") return field.fieldType;
    return this.SCALAR_MAP[field.fieldType] ?? field.fieldType.toLowerCase();
  }

  private renderField(field: ProtoField, number: number): string {
    let type = this.fieldProtoType(field);
    if (field.map) {
      const keyType = field.mapKeyType ?? "string";
      const valueType = field.mapValueType ?? this.fieldProtoType(field);
      type = `map<${keyType}, ${valueType}>`;
    } else if (field.repeated) {
      type = `repeated ${type}`;
    }
    const lines: string[] = [];
    if (field.description !== undefined && field.description !== "") {
      lines.push(`  // ${field.description}`);
    }
    lines.push(`  ${type} ${field.name} = ${number};`);
    return lines.join("\n");
  }

  private renderMessage(message: ProtoMessage): string {
    const lines: string[] = [];
    if (message.description !== undefined && message.description !== "") {
      lines.push(`// ${message.description}`);
    }
    lines.push(`message ${message.name} {`);
    message.fields.forEach((field, index) => {
      lines.push(this.renderField(field, index + 1));
    });
    lines.push("}");
    return lines.join("\n");
  }
}