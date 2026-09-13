using System;
using System.Threading.Tasks;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExamplePlugin;

/// <summary>
/// Provides minimal example implementation of an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// The plugin demonstrates the basic structure required to implement
/// <see cref="IAuthKitPlugin"/> and declare plugin metadata using
/// <see cref="PluginMetadataAttribute"/>.
/// </para>
/// <para>
/// This implementation does not register any additional services and always
/// reports healthy state when its health check is executed.
/// </para>
/// </remarks>
[PluginMetadata(
    Id = "example-plugin",
    Name = "Example Plugin",
    Description = "An example plugin for AuthKit.",
    Version = "1.0.0",
    Tags = new[] { "example" },
    Capabilities = new[] { "example" },
    DependsOn = new string[0]
)]
public class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Gets the unique identifier of the plugin.
    /// </summary>
    public string Id => "example-plugin";

    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    public string Name => "Example Plugin";

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    public string Description => "An example plugin for AuthKit.";

    /// <summary>
    /// Gets the semantic version of the plugin.
    /// </summary>
    public SemanticVersion Version => new(1, 0, 0);

    /// <summary>
    /// Registers services required by the plugin.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services. </param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing the application configuration. </param>
    public void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Register services here.
    }

    /// <summary>
    /// Performs a structured, cancellable health check for the plugin.
    /// </summary>
    /// <param name="services">The service provider used to resolve health check dependencies.</param>
    /// <param name="cancellationToken">A token that can cancel the health check.</param>
    /// <returns>A task containing the plugin health results.</returns>
    public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
            new[] { new PluginHealthResult(PluginHealthStatus.Healthy) });
    }
}
