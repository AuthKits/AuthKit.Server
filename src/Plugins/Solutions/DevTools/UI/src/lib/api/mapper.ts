/**
 * Mapper for converting wire format to domain format.
 * Transforms normalized wire data into application domain models.
 */

import type { GrpcMethod, GrpcService, ProtoField } from "../types";
import type { WireField, WireMessage, WireMethod, WireService } from "./normalizer";

function optionalString(value: unknown): string | undefined {
  return value !== undefined && value !== null ? String(value) : undefined;
}

export function mapService(wire: WireService, serviceName: string): GrpcService {
  return {
    name: wire.Name,
    package: wire.Package,
    description: optionalString(wire.Description),
    isPlugin: wire.IsPlugin ?? false,
    methods: wire.Methods.map((m) => mapMethod(m, wire)),
  };
}

export function mapMethod(wire: WireMethod, service: WireService): GrpcMethod {
  return {
    name: wire.Name,
    fullName: wire.FullName,
    description: optionalString(wire.Description),
    kind: resolveKind(wire.IsClientStreaming, wire.IsServerStreaming),
    requestType: wire.Request?.Name ?? "Request",
    responseType: wire.Response?.Name ?? "Response",
    fullRequestPath: wire.Request?.FullName ?? `${service.Package}.${service.Name}.Request`,
    fullResponsePath: wire.Response?.FullName ?? `${service.Package}.${service.Name}.Response`,
    requestFields: wire.Request ? mapMessage(wire.Request).fields : undefined,
    responseFields: wire.Response ? mapMessage(wire.Response).fields : undefined,
  };
}

export function mapMessage(wire: WireMessage): { name: string; fullName: string; description?: string; fields: ProtoField[] } {
  return {
    name: wire.Name,
    fullName: wire.FullName,
    description: optionalString(wire.Description),
    fields: wire.Fields.map(mapField),
  };
}

export function mapField(wire: WireField): ProtoField {
  return {
    number: wire.Number,
    name: wire.Name,
    description: optionalString(wire.Description),
    fieldType: wire.FieldType,
    repeated: wire.IsRepeated,
    map: wire.IsMap,
    mapKeyType: wire.MapKeyType,
    mapValueType: wire.MapValueType,
    enumValues: wire.EnumValues,
    message: wire.Message ? mapMessage(wire.Message) as any : undefined,
  };
}

function resolveKind(isClientStreaming: boolean, isServerStreaming: boolean): "unary" | "server streaming" | "client streaming" | "bidi streaming" {
  if (isClientStreaming && isServerStreaming) return "bidi streaming";
  if (isClientStreaming) return "client streaming";
  if (isServerStreaming) return "server streaming";
  return "unary";
}