const TOKEN_RE =
  /("(?:\\.|[^"\\])*")(\s*:)?|\b(true|false)\b|\bnull\b|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g;

export function highlightJson(input: string): string {
  return input.replace(
    TOKEN_RE,
    (match, stringValue: string | undefined, colon: string | undefined, ...rest: unknown[]) => {
      if (stringValue !== undefined) {
        const isKey = colon !== undefined;
        if (isKey) {
          return `<span class="tok-key">${escapeHtml(stringValue)}</span>${colon ?? ""}`;
        }
        const tokenClass = /^<[^>]*>$/.test(stripQuotes(stringValue))
          ? "tok-ph"
          : "tok-string";
        return `<span class="${tokenClass}">${escapeHtml(stringValue)}</span>${colon ?? ""}`;
      }
      const literal = rest[0] as string | undefined;
      if (literal === "true" || literal === "false") {
        return `<span class="tok-bool">${literal}</span>`;
      }
      if (rest[1] !== undefined) return `<span class="tok-keyword">null</span>`;
      return `<span class="tok-number">${match}</span>`;
    },
  );
}

const PROTO_TOKEN_RE =
  /("(?:\\.|[^"\\])*")|(\/\/.*$)|\b(syntax|package|message|service|rpc|returns|import|option|repeated|enum|map)\b|\b(string|int32|int64|uint32|uint64|sint32|sint64|fixed32|fixed64|sfixed32|sfixed64|bool|bytes|float|double)\b/gm;

export function highlightProto(input: string): string {
  const escaped = escapeHtml(input);
  return escaped.replace(
    PROTO_TOKEN_RE,
    (match, strVal, commentVal, keywordVal, typeVal) => {
      if (strVal !== undefined) {
        return `<span class="tok-string">${strVal}</span>`;
      }
      if (commentVal !== undefined) {
        return `<span class="text-muted/60 italic">${commentVal}</span>`;
      }
      if (keywordVal !== undefined) {
        return `<span class="text-accent2 font-semibold">${keywordVal}</span>`;
      }
      if (typeVal !== undefined) {
        return `<span class="text-[#7dd3fc] font-medium">${typeVal}</span>`;
      }
      return match;
    },
  );
}

function stripQuotes(value: string): string {
  return value.length >= 2 ? value.slice(1, -1) : value;
}

export function toHighlightedJson(value: unknown): string {
  return highlightJson(typeof value === "string" ? value : JSON.stringify(value, null, 2));
}

export function escapeHtml(input: string): string {
  return input.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

export interface CodeLine {
  number: number;
  html: string;
  raw: string;
}

export function formatJsonLines(value: unknown): CodeLine[] {
  const json = typeof value === "string" ? value : JSON.stringify(value, null, 2);
  const highlighted = highlightJson(json);
  const lines = highlighted.split("\n");
  const rawLines = json.split("\n");
  return lines.map((html, i) => ({
    number: i + 1,
    html,
    raw: rawLines[i] ?? "",
  }));
}

export function formatRawLines(value: string): CodeLine[] {
  const lines = value.split("\n");
  return lines.map((line, i) => ({
    number: i + 1,
    html: escapeHtml(line),
    raw: line,
  }));
}

export function formatProtoStruct(val: unknown, depth = 0): string {
  if (val === null || val === undefined) return '<span class="tok-keyword">null</span>';
  if (typeof val === "string") return `<span class="tok-string">"${escapeHtml(val)}"</span>`;
  if (typeof val === "number") return `<span class="tok-number">${val}</span>`;
  if (typeof val === "boolean") return `<span class="tok-bool">${val}</span>`;
  if (Array.isArray(val)) {
    if (val.length === 0) return "[]";
    const indent = "  ".repeat(depth + 1);
    const closeIndent = "  ".repeat(depth);
    const items = val.map((v) => `${indent}${formatProtoStruct(v, depth + 1)}`).join(",\n");
    return `[\n${items}\n${closeIndent}]`;
  }
  if (typeof val === "object") {
    const keys = Object.keys(val as object);
    if (keys.length === 0) return "{\n}";
    const indent = "  ".repeat(depth + 1);
    const closeIndent = "  ".repeat(depth);
    const lines = keys.map((k) => {
      const v = (val as Record<string, unknown>)[k];
      return `${indent}<span class="text-[#7dd3fc]">${escapeHtml(k)}</span>: ${formatProtoStruct(v, depth + 1)}`;
    }).join(",\n");
    return `{\n${lines}\n${closeIndent}}`;
  }
  return escapeHtml(String(val));
}
