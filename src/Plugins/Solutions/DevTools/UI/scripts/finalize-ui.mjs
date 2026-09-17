import { rename } from "node:fs/promises";
import { join } from "node:path";

const dist = join(import.meta.dirname, "..", "dist");

await rename(
  join(dist, "template.html"),
  join(dist, "ui.html"),
);
