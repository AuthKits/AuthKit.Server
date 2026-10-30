using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using AuthKit.Plugins.Abstractions.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shield.Composition;
using Shield.Endpoints;
using Shield.Grpc;
using Shield.Health;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace Shield;

/// <summary>
/// Critical-security plugin: detects attack traffic through ordered detector
/// strategies, blocks offending clients, and can block all HTTP and gRPC
/// traffic after distributed signals. Thin composition root — services, pipeline declarations,
/// endpoints, and health each live in their own unit.
/// </summary>
[PluginMetadata(
    id: "authkit.shield",
    version: "1.0.0",
    tags: ["security", "critical"],
    dependsOn: [],
    capabilities: ["security"],
    name: "Shield",
    displayName: "Shield",
    description: "Attack detection with client blocking and full-traffic lockdown for HTTP and gRPC.",
    author: "AuthKit Contributors",
    license: "MIT",
    licenseUrl: "https://opensource.org/licenses/MIT",
    homepage: "https://github.com/AuthKits/AuthKit.Server",
    repositoryUrl: "https://github.com/AuthKits/AuthKit.Server"
)]
public sealed class ShieldPlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers Shield options, state, and detectors.
    /// </summary>
    public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) =>
        services.AddShield(context.Configuration);

    /// <summary>
    /// Declares the HTTP kill-switch and the gRPC interceptor.
    /// </summary>
    public IReadOnlyList<PluginMiddleware> Middlewares => Pipeline.ShieldPipeline.Declarations;

    /// <summary>
    /// Maps the operator HTTP and gRPC endpoints.
    /// </summary>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ShieldEndpoints.Map(endpoints);
        endpoints.MapGrpcService<ShieldStatusService>();
    }

    /// <summary>
    /// Reports unhealthy while a lockdown is active.
    /// </summary>
    public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        Task.FromResult(ShieldHealth.Report(services, cancellationToken));
}
