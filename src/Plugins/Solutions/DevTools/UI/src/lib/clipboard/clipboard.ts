/**
 * Copy text to the system clipboard. Throws on failure so callers can decide
 * how to surface (or ignore) the error.
 */
export async function copyText(text: string): Promise<void> {
  await navigator.clipboard.writeText(text);
}