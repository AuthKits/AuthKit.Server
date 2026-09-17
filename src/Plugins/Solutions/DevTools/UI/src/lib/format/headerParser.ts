/**
 * Header parser for gRPC metadata.
 * Handles parsing of key-value header strings.
 */

export interface HeaderParser {
  parse(text: string): Record<string, string>;
}

export class GrpcHeaderParser implements HeaderParser {
  parse(text: string): Record<string, string> {
    const headers: Record<string, string> = {};
    for (const line of text.split(/\r?\n/)) {
      const match = /^\s*([^:]+):\s*(.*)\s*$/.exec(line);
      if (match !== null) headers[match[1].trim()] = match[2].trim();
    }
    return headers;
  }
}