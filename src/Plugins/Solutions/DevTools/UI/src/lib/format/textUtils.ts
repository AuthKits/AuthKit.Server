/**
 * Text utilities for editor display.
 * Handles line number generation and editor height calculation.
 */

export interface TextUtils {
  generateLineNumbers(text: string): string;
  calculateEditorHeight(text: string, options?: { min?: number; max?: number }): string;
}

export class DefaultTextUtils implements TextUtils {
  generateLineNumbers(text: string): string {
    return text
      .split("\n")
      .map((_, index) => index + 1)
      .join("\n");
  }

  calculateEditorHeight(
    text: string,
    options: { min?: number; max?: number } = {},
  ): string {
    const { min = 4, max = 24 } = options;
    const rows = Math.max(min, Math.min(max, text.split("\n").length));
    return `${rows * 20 + 24}px`;
  }
}