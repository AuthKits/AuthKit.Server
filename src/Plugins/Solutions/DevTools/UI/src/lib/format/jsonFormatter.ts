/**
 * JSON formatter and error formatter.
 * Handles JSON pretty-printing and error message formatting.
 */

export interface JsonFormatter {
  prettyPrint(text: string): string;
  formatError(error: unknown): string;
}

export class DefaultJsonFormatter implements JsonFormatter {
  prettyPrint(text: string): string {
    try {
      return JSON.stringify(JSON.parse(text), null, 2);
    } catch {
      return text;
    }
  }

  formatError(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}