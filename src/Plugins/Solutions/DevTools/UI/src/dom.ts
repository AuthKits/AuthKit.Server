/** Returns a required page element by id. */
export function getById<T extends HTMLElement = HTMLElement>(id: string): T
{
  const node = document.getElementById(id);
  if (node === null) throw new Error(`Element #${id} not found.`);
  return node as T;
}

/** Creates an element with optional class and text content. */
export function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string,
  text?: string,
): HTMLElementTagNameMap[K]
{
  const node = document.createElement(tag);
  if (className !== undefined) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}
