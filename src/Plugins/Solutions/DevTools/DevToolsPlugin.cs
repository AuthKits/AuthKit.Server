using DevTools.Options;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using DevTools.Catalog;
using DevTools.Middleware;
using DevTools.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevTools;

/// <summary>
/// AuthKit developer tools plugin responsible for exposing Swagger UI for the
/// REST API and a web interface for browsing and invoking gRPC services.
/// </summary>
/// <remarks>
/// <para>
/// The plugin serves the Swagger UI for the host's OpenAPI document and
/// provides a single-page interface for inspecting gRPC services, their
/// methods, and messages, and executing unary requests.
/// </para>
/// <para>
/// The Host core provides the OpenAPI document itself and the gRPC services
///  through dependency injection. The plugin does not generate Swagger
/// documents and does not require generated gRPC client stubs; methods are
/// invoked dynamically using the protobuf descriptors returned by the
/// catalog.
/// </para>
/// </remarks>
[PluginMetadata(
    id: "authkit.devtools",
    version: "1.0.0",
    tags: ["developer", "tools", "grpc", "swagger"],
    dependsOn: [],
    capabilities: ["developer-tools"],
    name: "DevTools",
    displayName: "Developer Tools",
    description: "Swagger UI for REST and web UI for gRPC services.",
    author: "AuthKit Contributors",
    license: "MIT",
    licenseUrl: "https://opensource.org/licenses/MIT",
    homepage: "https://example.org/devtools",
    repositoryUrl: "https://example.org/devtools.git"
)]
public sealed class DevToolsPlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers the gRPC service catalog, invocation runtime, and Swagger
    /// serving components in the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services.</param>
    /// <param name="configuration">Application configuration used to configure <see cref="DevToolsOptions"/>.</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DevToolsOptions>(configuration.GetSection("DevTools"));

        services.AddSingleton<IGrpcServiceCatalog, GrpcServiceCatalog>();
        services.AddSingleton<GrpcDynamicInvoker>();
        services.AddSingleton<SwaggerHost>();
    }

    /// <summary>
    /// The middleware serving the Swagger UI and the gRPC UI together with
    /// their JSON APIs.
    /// </summary>
    public Type MiddlewareType => typeof(DevToolsMiddleware);

    /// <summary>
    /// Verifies that the gRPC service catalog is resolvable and can be
    /// enumerated.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <returns><c>true</c> when the catalog is available; otherwise, <c>false</c>. </returns>
    public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var catalog = services.GetService<IGrpcServiceCatalog>();
        if (catalog is null)
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Unhealthy, "gRPC service catalog is unavailable.")]);

        try
        {
            _ = catalog.GetServices();
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Healthy, "gRPC service catalog is available.")]);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Unhealthy, "gRPC service catalog is unavailable.")]);
        }
    }
}