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
    /// Performs a health check for the plugin.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> used to resolve services required for the health check. </param>
    /// <returns>
    /// A task containing <see langword="true"/> when the plugin is healthy;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public Task<bool> CheckHealthAsync(IServiceProvider services)
        => Task.FromResult(true);
}
