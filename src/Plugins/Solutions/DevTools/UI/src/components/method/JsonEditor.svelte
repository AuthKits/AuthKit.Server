<script lang="ts">
  import { highlightJson } from "../../lib/format/jsonHighlight";
  import { editorHeight, lineNumbers } from "../../lib/format/format";

  let {
    value,
    onChange,
    payloadSizeBytes = $bindable(0),
  }: {
    value: string;
    onChange: (next: string) => void;
    payloadSizeBytes?: number;
  } = $props();

  let textarea: HTMLTextAreaElement;
  let backdrop: HTMLPreElement;
  let gutter: HTMLPreElement;

  $effect(() => {
    payloadSizeBytes = new Blob([value]).size;
  });

  const lineNumbersText = $derived(lineNumbers(value));
  const highlighted = $derived(highlightJson(value));
  const height = $derived(editorHeight(value));

  function syncScroll(): void {
    if (!textarea || !backdrop || !gutter) return;
    backdrop.scrollTop = textarea.scrollTop;
    backdrop.scrollLeft = textarea.scrollLeft;
    gutter.scrollTop = textarea.scrollTop;
  }
</script>

<div
  class="flex min-h-[104px] max-h-[504px] overflow-hidden rounded-lg border border-border bg-bg"
  style="height: {height}"
>
  <pre
    bind:this={gutter}
    aria-hidden="true"
    class="pointer-events-none m-0 shrink-0 select-none overflow-hidden border-r border-border bg-panel2 px-2 py-3 text-right font-mono text-[12.5px] leading-[20px] text-muted"
  >{lineNumbersText}</pre>

  <div class="relative min-w-0 flex-1">
    <pre
      bind:this={backdrop}
      aria-hidden="true"
      class="pointer-events-none absolute inset-0 m-0 overflow-auto p-3 font-mono text-[12.5px] leading-[20px] whitespace-pre text-muted"
    >{@html highlighted}</pre>
    <textarea
      bind:this={textarea}
      bind:value
      spellcheck="false"
      autocomplete="off"
      wrap="off"
      oninput={syncScroll}
      onscroll={syncScroll}
      onchange={() => onChange(value)}
      class="absolute inset-0 resize-none overflow-auto whitespace-pre bg-transparent p-3 font-mono text-[12.5px] leading-[20px] text-transparent caret-accent outline-none selection:bg-accent/25"
    ></textarea>
  </div>
</div>

<style>
  textarea::selection {
    background: rgb(166 107 61 / 0.25);
  }
</style>