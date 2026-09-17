/**
 * Byte calculator for text size calculations.
 * Handles byte size calculation and readable formatting.
 */

export interface ByteCalculator {
  calculateByteSize(text: string): number;
  formatFriendlyBytes(bytes: number): string;
}

export class TextByteCalculator implements ByteCalculator {
  calculateByteSize(text: string): number {
    return new TextEncoder().encode(text).length;
  }

  formatFriendlyBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }
}
