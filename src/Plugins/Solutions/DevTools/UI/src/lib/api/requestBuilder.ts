/**
 * Request builder for creating default request JSON.
 * Handles construction of default values for gRPC method requests.
 */

import type { GrpcMethod, ProtoField } from "../types";

export interface RequestBuilder {
  buildDefaultRequestJson(method: GrpcMethod): string;
  buildDefaultResponseJson(method: GrpcMethod): Record<string, unknown>;
}

export class DefaultRequestBuilder implements RequestBuilder {
  buildDefaultRequestJson(method: GrpcMethod): string {
    if (method.requestFields !== undefined && method.requestFields.length > 0) {
      return JSON.stringify(this.buildMessageValue(method.requestFields), null, 2);
    }
    return "{}";
  }

  buildDefaultResponseJson(method: GrpcMethod): Record<string, unknown> {
    const noun = method.responseType.split(".").pop() ?? "response";
    return { [this.camelCase(noun)]: "OK", message: `Hello from ${method.name}!` };
  }

  private buildMessageValue(fields: readonly ProtoField[], depth = 0): Record<string, unknown> {
    const message: Record<string, unknown> = {};
    for (const field of fields) {
      message[field.name] = this.buildFieldValue(field, depth);
    }
    return message;
  }

  private buildFieldValue(field: ProtoField, depth = 0): unknown {
    if (field.map) return {};
    if (field.repeated) return [];
    if (field.message) {
      if (depth >= 8) return null;
      return this.buildMessageValue(field.message.fields, depth + 1);
    }
    if (field.enumValues && field.enumValues.length > 0) return field.enumValues[0];
    const type = field.fieldType.toLowerCase();
    if (type === "bool" || type === "boolean") return false;
    if (type.includes("int") || type.includes("float") || type.includes("double") || 
        type.includes("uint") || type.includes("fixed") || type.includes("sint") || 
        type.includes("sfixed") || type.includes("long") || type.includes("decimal")) return 0;
    if (type === "bytes" || type.includes("character")) return "";
    return "";
  }

  private camelCase(value: string): string {
    return value.replace(/^[A-Z]/, (m) => m.toLowerCase());
  }
}