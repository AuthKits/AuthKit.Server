import { copyText } from "./clipboard";

/**
 * Shared clipboard state for components that show a transient "Copied" badge.
 * Returns the currently copied section id and a `copy` helper that reports
 * success by setting that id for 1.5 seconds.
 */
export function useClipboard() {
  let copiedSection = $state<string | null>(null);

  async function copy(text: string, section: string): Promise<void> {
    try {
      await copyText(text);
      copiedSection = section;
      setTimeout(() => {
        if (copiedSection === section) copiedSection = null;
      }, 1500);
    } catch {
      // ignore clipboard errors selection still works manually
    }
  }

  return {
    get copiedSection() {
      return copiedSection;
    },
    copy,
  };
}
