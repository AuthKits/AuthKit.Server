# Custom Plugins

Every behavior in AuthKit that isn't Core domain or host plumbing arrives as a plugin including the built-in [DevTokens](/plugins/devtokens/) and [DevTools](/plugins/devtools/). Writing your own follows the same path the built-ins took: implement one contract, declare metadata, drop the assembly in a directory. The host does the rest.

## Why Plugins Instead of Forks

> AuthKit's cost center is contracts between layers, not single classes. Forking the host to add behavior would tangle your code with ours on every update. The plugin contract inverts that: the host exposes stable slots, and your assembly fills them without referencing host internals. When the host evolves, the contract is what stays still.

## The Contract

`IAuthKitPlugin` is partial interface one facet per concern, grouped in `IAuthKitPlugin*.cs` files: base identity and metadata accessors, host requirements, configuration, health, pipeline, security, and lifecycle hooks. You implement the facets you need the rest comes with defaults derived from your metadata.

In practice a minimal plugin looks like the bundled example:

```csharp
[PluginMetadata(
    id: "acme.audit",
    version: "1.0.0",
    tags: ["audit"],
    dependsOn: [],
    capabilities: ["audit"],
    name: "Audit",
    displayName: "Audit Trail",
    description: "Appends audit entries for token operations.",
    author: "Acme",
    license: "MIT")]
public sealed class AuditPlugin : IAuthKitPlugin
{
    public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
    {
        // register plugin services here and only here
    }
}
```

Two rules keep the model honest:

- **Stay inside the contract** keep host integration limited to what the contract exposes reach past it and you own the breakage.
- **Register in ConfigureServices** register every plugin-specific dependency inside `ConfigureServices` the host never wires your internals for you.

## Metadata Is Load-Bearing

`[PluginMetadata]` is not decoration. The host reads it before loading anything and uses it for:

- **identity** `Id` must be non-empty, stable across restarts, and host-unique (format and uniqueness are validated before activation) `Version` is real SemVer with parsing and precedence
- **presentation** `DisplayName ?? Name` plus `Description` surface in startup output, diagnostics, and UIs
- **capabilities** case insensitive feature names (`plugin.Supports("auth")`) used for pre-activation checks and post load consistency validation
- **tags** case sensitive classification for filtering in catalogs and UIs.

A plugin class without the attribute fails fast with `InvalidOperationException` loud at startup instead of mysterious at runtime.

:::warning[Don't bypass the contract]
Reaching past the contract (reflection into host internals, private APIs) is the one way to void the stability the plugin model buys you. If slot you need doesn't exist, extend the contract instead of routing around it.
:::

## From Directory to Running Code

Dropping an assembly in the configured directory starts a pipeline (see [ADR-009](/adr/009-dynamic-plugin-discovery/) and [ADR-010](/adr/010-plugin-loading-from-directory/)):

1. **Discovery** the host scans the directory at startup nothing needs a host reference or recompilation.
2. **Manifest & gates** metadata is read into a manifest. `IsEnabled: false` skips the plugin before loading (no consistency check runs for it) accepted manifests go through format, uniqueness, and consistency validation.
3. **Activation order** dependencies first (topological sort wins over everything), then `Priority` ascending among the ready set. Lower numbers start earlier the default is `0`.
4. **Configuration** each plugin receives its scoped configuration section plus host-builder access through the plugin configuration context ([ADR-022](/adr/022-plugin-configuration-context-and-builder/)).
5. **Pipeline & lifecycle** endpoints and middleware attach through explicit host hooks ([ADR-023](/adr/023-plugin-application-pipeline-hooks/)) startup and shutdown follow the standard .NET host lifecycle ([ADR-024](/adr/024-plugin-lifecycle-and-hosted-services/)).
6. **Health & OpenAPI** structured health checks ([ADR-029](/adr/029-structured-plugin-health-contract/)) and security schemes flow into monitoring and the DevTools Swagger automatically.

## Start From the Example

:::tip[Copy, don't start blank]
`src/Plugins/Solutions/ExamplePlugin/` is a minimal greeting plugin (`authkit.example.ExampleGreeter` over gRPC) exercising exactly this path: metadata, discovery, configuration, endpoint. Copy the directory, rename the id, and grow from something that already loads.
:::

## gRPC Services

Plugins are not limited to REST: plugin can expose its own gRPC services next to the host ones, mapped through the same discovery. The example plugin does exactly that with `ExampleGreeter/SayHello` see the [gRPC surface](/reference/grpc/) and [plugin services](/reference/grpc/#Plugin-Services).
