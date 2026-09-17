import { formatRawLines, type CodeLine } from "./jsonHighlight";

export type RawFormat = "base64" | "hex" | "raw";

/**
 * Render the raw wire body (base64 from the server, or synthesized from the
 * decoded JSON) as plain text in the requested format.
 */
export function formatRaw(
  body: unknown,
  rawBase64: string | undefined,
  format: RawFormat,
): string {
  if (format === "base64") return rawBase64 ?? bodyToBase64(body);
  if (format === "hex") return toHex(rawBase64 ?? bodyToBase64(body));
  return JSON.stringify(body);
}

export function rawCodeLines(
  body: unknown,
  rawBase64: string | undefined,
  format: RawFormat,
): CodeLine[] {
  return formatRawLines(formatRaw(body, rawBase64, format));
}

function bodyToBase64(body: unknown): string {
  return btoa(JSON.stringify(body));
}

function toHex(base64: string): string {
  try {
    const bin = atob(base64);
    const hex = Array.from(bin)
      .map((c) => c.charCodeAt(0).toString(16).padStart(2, "0"))
      .join(" ");
    const chunks: string[] = [];
    for (let i = 0; i < hex.length; i += 48) {
      chunks.push(hex.slice(i, i + 48).trim());
    }
    return chunks.join("\n");
  } catch {
    return "00 00 00";
  }
}