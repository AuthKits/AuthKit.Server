# DevTools

When AuthKit runs, you don't want to guess what it exposes you want to see it. The `DevTools` plugin is the server's built-in inspection hatch: it hosts developer tooling inside the running process, so the same binary that serves your SDK clients also answers the question "what is actually mapped right now?".

That question is harder than it sounds. AuthKit composes its surface at startup from the host plus every discovered plugin: REST endpoints, gRPC services, OpenAPI security schemes, health checks. A checked-in spec file would rot within a week. DevTools avoids that by generating everything from the composed application at runtime what you browse is always what is really there.

## What You Get

Open a running AuthKit and three doors are waiting (all paths configurable, see [Options](#Options)):

<script>
  import IntroCards from '$lib/IntroCards.svelte'

  const doorsCards = [
    { icon: 'terminal', title: '/devtools', desc: 'Embedded DevTools UI.', href: '#UI-That-Ships-With-the-Plugin' },
    { icon: 'code', title: '/devtools/swagger', desc: 'Swagger UI (DocumentName: v1, AuthKit API title).', href: '#Swagger-via-Reflection' },
    { icon: 'sync', title: '/grpc-ui', desc: 'gRPC exploration UI.', href: '#Exploring-gRPC-Without-Clients' },
  ]
</script>

<IntroCards cards={doorsCards} />

A typical session flows like this:

1. Start the stack, open `/devtools/swagger`, and walk the token endpoints from the [REST API Reference](/reference/rest-api/) issue developer token, then verify it, without writing a single line of client code.
2. When something behaves unexpectedly, switch to `/grpc-ui` and invoke the underlying `jwks` or `monitoring` calls directly.
3. Compare the two: if gRPC answers cleanly, the problem sits in the transport if not, it lives deeper.

## Swagger via Reflection

Behind `/devtools/swagger` stands `SwaggerHost`. The host already generates an OpenAPI document through its generation pipeline (SwaggerGen) the plugin's job is to serve that document and the UI from inside the plugin, contributed back into the host pipeline like any other middleware.

Two details matter here. First, the document covers plugin-contributed endpoints and security schemes as well your own plugin declaring an OpenAPI scheme shows up in Swagger with zero extra work. Second, the reflection-based middleware instances are constructed once and reused for every request, so serving docs doesn't pay a per-request reflection tax. See [ADR-021](/adr/021-swagger-serving-via-reflection/).

:::tip[Free for your plugin]
If you write your own plugin, you do nothing to land in Swagger just declare schemes through the contract. Details: [Custom plugins](/plugins/custom/).
:::

## Exploring gRPC Without Clients

gRPC is powerful but awkward to poke at: you normally need generated stubs in your language before you can call anything. `GrpcServiceCatalog` removes that friction. It tracks every mapped gRPC service host services like `jwks` and `monitoring` as well as plugin-contributed ones together with their protobuf descriptors and the doc comments from the `.proto` files. That catalog is what the [gRPC surface](/reference/grpc/) documentation is derived from, and what the UI browses.

`GrpcDynamicInvoker` goes one step further: it actually executes unary calls discovered through the catalog. Messages are marshaled using the server-provided descriptors, so no compiled client code is needed anywhere in the loop, and the outcome comes back as serializable `GrpcInvocationResult` carrying `Success`, `StatusCode`, `StatusName`, `Detail`, and the payload.

Every tool has edges know these three before you lean on the invoker:

- **unary only**: client streaming and server streaming methods are reported as unsupported rather than half executed
- **unknown methods fail cleanly**: a wrong `service/method` pair returns `NotFound` in the result instead of throwing
- **relaxed TLS**: certificate validation is loosened because the host runs on development certificate exactly what you want locally, and exactly what you don't want to copy into production tooling.

:::warning[Local only]
The invoker deliberately relaxes TLS for the host's development certificate. Calling remote production services over the same channel misses the point use full verification there.
:::

## UI That Ships With the Plugin

The DevTools frontend (`src/Plugins/Solutions/DevTools/UI/`) is modular TypeScript application Svelte 5 with Tailwind compiled down to single embedded resource. In practice that means the UI travels inside the plugin assembly: deploying AuthKit never involves separate static file step, and the UI version can never drift from the server version it inspects.

### What the UI Does

- **Service tree** sidebar with every cataloged gRPC service, browsable by package and service
- **Method invocation** pick unary method, fill the request payload, and execute it through the same `GrpcDynamicInvoker` the backend exposes
- **Proto docs** request/response shapes rendered from the server provided descriptors and doc comments, so you read the contract you actually call.

### How It Is Built

`vite build` bundles the app, `vite-plugin-singlefile` collapses it into one self-contained HTML file, and `scripts/finalize-ui.mjs` prepares it for embedding. Type safety is enforced up front with `svelte-check`. No separate web server, no CDN assets, no version skew: one assembly, one UI, always matching the backend it ships with.

## How It Attaches

`DevToolsMiddleware` follows the same rule as all plugin middleware: it answers only the paths that belong to it and lets everything else pass through untouched. Attachment happens through the host's explicit pipeline hooks, so enabling or disabling DevTools never changes how the core request path behaves see [ADR-023](/adr/023-plugin-application-pipeline-hooks/).

## Options

Everything above is driven by `DevToolsOptions` in the plugin's scoped configuration section:

| Option | Default | Description |
|--------|---------|-------------|
| `PathBase` | `/devtools` | Base path for the UI and Swagger |
| `GrpcUiPathBase` | `/grpc-ui` | Base path for the gRPC UI |
| `GrpcTarget` | Override for the invoked gRPC target |
| `Swagger.RoutePrefix` | `swagger` | Swagger UI route under `PathBase` |
| `Swagger.DocumentName` | `v1` | Served OpenAPI document name |
| `Swagger.DocumentTitle` | `AuthKit API` | Swagger UI title |
| `Swagger.Enabled` | `null` (Swagger only in Development) | Toggle for the Swagger UI |

Change path when it collides with your own routes point `GrpcTarget` elsewhere when the UI should invoke a different host than the one serving it.
