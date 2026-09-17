import type { GrpcService } from "../types";
import { GrpcHeaderParser } from "./headerParser";
import { DefaultJsonFormatter } from "./jsonFormatter";
import { TextByteCalculator } from "./byteCalculator";
import { DefaultTextUtils } from "./textUtils";
import { DefaultProtoGenerator } from "./protoGenerator";

// Create default instances for backward compatibility
const headerParser = new GrpcHeaderParser();
const jsonFormatter = new DefaultJsonFormatter();
const byteCalculator = new TextByteCalculator();
const textUtils = new DefaultTextUtils();
const protoGenerator = new DefaultProtoGenerator();

// Backward compatible function exports
export function parseHeaders(text: string): Record<string, string> {
  return headerParser.parse(text);
}

export function prettyPrint(text: string): string {
  return jsonFormatter.prettyPrint(text);
}

export function formatError(error: unknown): string {
  return jsonFormatter.formatError(error);
}

export function byteSize(text: string): number {
  return byteCalculator.calculateByteSize(text);
}

export function lineNumbers(text: string): string {
  return textUtils.generateLineNumbers(text);
}

/**
 * Scales the code block height to the request content (line count) so the
 * Request JSON block grows and shrinks with the request instead of keeping a
 * fixed height and scrolling internally. Clamped to readable range.
 */
export function editorHeight(
  text: string,
  options: { min?: number; max?: number } = {},
): string {
  return textUtils.calculateEditorHeight(text, options);
}

export function friendlyBytes(bytes: number): string {
  return byteCalculator.formatFriendlyBytes(bytes);
}

export function protoPreview(service: GrpcService): string {
  return protoGenerator.generatePreview(service);
}

// Export classes for dependency injection
export { GrpcHeaderParser, type HeaderParser } from "./headerParser";
export { DefaultJsonFormatter, type JsonFormatter } from "./jsonFormatter";
export { TextByteCalculator, type ByteCalculator } from "./byteCalculator";
export { DefaultTextUtils, type TextUtils } from "./textUtils";
export { DefaultProtoGenerator, type ProtoGenerator } from "./protoGenerator";
