import { join } from "node:path";
import tailwindcss from "@tailwindcss/vite";
import { svelte } from "@sveltejs/vite-plugin-svelte";
import { defineConfig } from "vite";
import { viteSingleFile } from "vite-plugin-singlefile";

export default defineConfig({
  root: import.meta.dirname,
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: join(import.meta.dirname, "template.html"),
    },
  },
  plugins: [svelte(), tailwindcss(), viteSingleFile()],
});