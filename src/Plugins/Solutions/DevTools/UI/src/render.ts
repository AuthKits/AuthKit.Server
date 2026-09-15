import { describeStreaming, fieldTypeText, jsonFromSchema, prettyPrint, streamingKind } from "./schema";
import { el } from "./dom";
import type { SelectedIndex } from "./store";
import type {
  GrpcFieldSchema,
  GrpcMessageSchema,
  GrpcMethodInfo,
  GrpcServiceInfo,
} from "./types";

export interface MethodDetail
{
  readonly request: HTMLTextAreaElement;
  readonly headers: HTMLTextAreaElement;
  setStatus(state: "idle" | "ok" | "err", text: string): void;
  writeResponse(text: string): void;
}

export function renderServiceList(
  container: HTMLElement,
  services: readonly GrpcServiceInfo[],
  selected: SelectedIndex | null,
  onSelect: (serviceIndex: number, methodIndex: number) => void,
): void
{
  container.replaceChildren();

  if (services.length === 0)
  {
    container.append(emptyMessage("No gRPC services found."));
    return;
  }

  for (const [serviceIndex, svc] of services.entries())
  {
    const details = el("details", "service");
    if (serviceIndex === 0) details.open = true;

    const summary = el("summary");
    summary.append(svc.package || svc.fileName);
    const count = el("span", "muted", `(${svc.methods?.length ?? 0})`);
    count.style.cssText = "font-weight:400;font-size:12px;";
    summary.append(count);
    details.append(summary);

    const list = el("ul", "methods");
    for (const [methodIndex, method] of (svc.methods ?? []).entries())
    {
      const isSelected = selected?.[0] === serviceIndex && selected?.[1] === methodIndex;
      const item = el("li", isSelected ? "active" : undefined);
      item.dataset.service = String(serviceIndex);
      item.dataset.method = String(methodIndex);

      const kind = streamingKind(method);
      const pill = el("span", `pill ${kind}`, method.name);
      pill.title = `${kind} streaming`;
      item.append(pill);
      item.addEventListener("click", () => onSelect(serviceIndex, methodIndex));

      list.append(item);
    }
    details.append(list);
    container.append(details);
  }
}

export function highlightSelectedMethod(selected: SelectedIndex | null): void
{
  for (const item of document.querySelectorAll<HTMLLIElement>(".methods li")) {
    const isActive = selected !== null
      && item.dataset.service === String(selected[0])
      && item.dataset.method === String(selected[1]);
    item.classList.toggle("active", isActive);
  }
}

export function renderMethodDetail(
  container: HTMLElement,
  method: GrpcMethodInfo,
  initialHeaders: string,
  onInvoke: (detail: MethodDetail) => void,
): void
{
  container.replaceChildren();
  const streaming = method.isClientStreaming || method.isServerStreaming;

  const methodPill = el("span", `pill ${method.isClientStreaming ? "cli" : "uni"}`, describeStreaming(method));
  container.append(card(heading(method.fullName, [methodPill]), [renderSchema("Request", method.request)]));
  container.append(card(heading("Response"), [renderSchema("Message", method.response)]));

  const headers = el("textarea");
  headers.placeholder = "Authorization: Bearer <token>\nX-Custom: value";
  headers.value = initialHeaders;
  container.append(card(heading("Metadata headers"), [headers]));

  const request = el("textarea");
  request.spellcheck = false;
  request.value = prettyPrint(jsonFromSchema(method.request));

  const defaultsButton = el("button", "secondary", "Fill defaults");
  const formatButton = el("button", "secondary", "Format JSON");
  const invokeButton = el("button", undefined, "Invoke");
  const status = el("span", "status");
  status.hidden = true;
  const response = el("pre");
  response.hidden = true;

  const group = el("div", "row");
  group.append(defaultsButton, formatButton, invokeButton, status);
  container.append(card(heading("Request JSON"), [request, group, response]));

  if (streaming)
  {
    invokeButton.disabled = true;
    invokeButton.title = "Streaming is not supported";
  }

  defaultsButton.addEventListener("click", () => {
    request.value = prettyPrint(jsonFromSchema(method.request));
  });
  formatButton.addEventListener("click", () => {
    request.value = prettyPrint(request.value);
  });

  const detail: MethodDetail = {
    request,
    headers,
    setStatus(state, text) {
      status.classList.remove("ok", "err");
      if (state === "ok" || state === "err") status.classList.add(state);
      status.hidden = false;
      status.textContent = text;
    },
    writeResponse(text) {
      response.hidden = false;
      response.textContent = text;
    },
  };

  invokeButton.addEventListener("click", () => onInvoke(detail));
}

function emptyMessage(text: string): HTMLDivElement
{
  const message = el("div", "empty", text);
  message.style.padding = "14px";
  return message;
}

function heading(text: string, additions: Node[] = []): HTMLHeadingElement
{
  const h = el("h2");
  h.append(text, ...additions);
  return h;
}

function card(h: HTMLHeadingElement, bodyNodes: (Node | string)[]): HTMLDivElement
{
  const card = el("div", "card");
  const body = el("div", "body");
  body.append(...bodyNodes);
  card.append(h, body);
  return card;
}

function renderField(field: GrpcFieldSchema, depth: number, into: HTMLElement): void
{
  const row = el("div", "row-line");
  const indent = el("span", "indent", "\u00A0".repeat(depth * 4));
  row.append(indent, `${field.name}: `, el("span", "type", fieldTypeText(field)));

  if (field.isRepeated && !field.isMap)
  {
    row.append(" ", el("span", "rep", "[repeated]"));
  }
  if (field.enumType)
  {
    row.append(" ", el("span", "enum", `${field.enumType} (${(field.enumValues ?? []).join("|")})`));
  }
  into.append(row);

  if (field.message && depth < 3) {
    for (const nested of field.message.fields ?? []) renderField(nested, depth + 1, into);
  }
}

function renderSchema(title: string, msg: GrpcMessageSchema | null | undefined): HTMLDivElement
{
  const wrap = el("div");
  if (!msg)
  {
    wrap.append(el("div", "empty", "—"));
    return wrap;
  }

  const fields = msg.fields ?? [];
  const schema = el("div", "schema");
  if (fields.length === 0) {
    schema.append(el("div", "empty", "(empty)"));
  }
  else
  {
    for (const field of fields) renderField(field, 0, schema);
  }

  wrap.append(el("h3", undefined, `${title} — ${msg.fullName}`), schema);
  return wrap;
}
