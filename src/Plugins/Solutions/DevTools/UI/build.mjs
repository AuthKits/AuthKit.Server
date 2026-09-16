import { build, context } from "esbuild";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

/** Marker replaced with the compiled JS bundle inside the HTML template. */
const BUNDLE_MARKER = "/*__BUNDLE__*/";

const root = dirname(fileURLToPath(import.meta.url));
const watch = process.argv.includes("--watch");

const options = {
  entryPoints: [join(root, "src", "app.ts")],
  bundle: true,
  format: "iife",
  minify: true,
  target: ["es2020"],
  outfile: join(root, "dist", "bundle.js"),
};

/** Writes the HTML template with the latest compiled bundle inlined. */
async function inlineBundle() {
  const [template, bundle] = await Promise.all([
    readFile(join(root, "template.html"), "utf8"),
    readFile(join(root, "dist", "bundle.js"), "utf8"),
  ]);
  const html = template.replace(BUNDLE_MARKER, () => bundle);
  await writeFile(join(root, "dist", "ui.html"), html);
}

if (watch) {
  const ctx = await context({
    ...options,
    plugins: [{
      name: "inline-bundle",
      /** Registers HTML inlining after each successful esbuild run. */
      setup(build) {
        build.onEnd(async (result) => {
          if (result.errors.length === 0) await inlineBundle();
        });
      },
    }],
  });
  await ctx.watch();
  console.log("Watching for changes…");
} else {
  await build(options);
  await inlineBundle();
  console.log("TypeScript UI written to dist/ui.html");
}
