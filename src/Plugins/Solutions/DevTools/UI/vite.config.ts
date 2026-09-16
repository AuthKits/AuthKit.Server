import { rename } from "node:fs/promises";
import { join, resolve } from "node:path";
import { defineConfig, type Plugin } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";

function outputAsUiHtml(): Plugin {
  return {
    name: "ui-html-output-name",
    apply: "build",
    async writeBundle() {
      const out = resolve(import.meta.dirname, "dist");
      await rename(join(out, "template.html"), join(out, "ui.html"));
    },
  };
}

export default defineConfig({
  root: resolve(import.meta.dirname),
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        ui: resolve(import.meta.dirname, "template.html"),
      },
    },
  },
  plugins: [viteSingleFile(), outputAsUiHtml()],
});