using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

/// <summary>
/// End-to-end example plugin exercising the full IAuthKitPlugin contract.
/// </summary>
/// <remarks>
/// <para>
/// This plugin is a living reference: it implements every hook the contract exposes so
/// authors can copy the parts their plugin needs. It is intentionally small and has no
/// external dependencies beyond ASP.NET Core and the AuthKit abstractions.
/// Each contract area lives in its own file — copy the file, not the class.
/// </para>
/// <list type="bullet">
/// <item>Metadata through <see cref="PluginMetadataAttribute"/> (identity, capabilities, dependencies) — this file.</item>
/// <item>Configuration through <c>ConfigureServices(IServiceCollection, AuthKitPluginContext)</c> — <c>Composition/ExampleServices.cs</c>.</item>
/// <item>Middleware through declarative <c>Middlewares</c>, <c>ConfigureApplication</c> and <c>ConfigurePipeline</c> — <c>Pipeline/ExamplePipeline.cs</c>.</item>
/// <item>Endpoints through <c>MapEndpoints</c> — <c>Endpoints/ExampleEndpoints.cs</c>.</item>
/// <item>Security schemes, authentication, and authorization — <c>Security/ExampleSecurity.cs</c>.</item>
/// <item>Structured health checks through <c>CheckHealthAsync</c> — <c>Health/ExampleHealth.cs</c>.</item>
/// <item>Lifecycle hooks and a plugin-owned hosted service — <c>Lifecycle/ExampleLifecycle.cs</c>.</item>
/// </list>
/// </remarks>
[PluginMetadata(
    id: "authkit.example",
    version: "1.0.0",
    tags: ["example", "reference", "template"],
    dependsOn: [],
    capabilities: ["example", "reference"],
    name: "ExamplePlugin",
    displayName: "Example Plugin",
    description: "Living reference implementing the full IAuthKitPlugin contract.",
    author: "AuthKit Contributors",
    license: "MIT",
    licenseUrl: "https://opensource.org/licenses/MIT",
    homepage: "https://example.org/example",
    repositoryUrl: "https://example.org/example.git",
    priority: 100,
    isEnabled: true,
    minHostVersion: "0.5.0"
)]
public sealed partial class ExamplePlugin : IAuthKitPlugin
{
}
