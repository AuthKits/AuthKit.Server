using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Extension methods for plugin capabilities and metadata.
/// </summary>
public static class PluginExtensions
{
    /// <summary>
    /// Binds strongly typed options from the plugin's standard configuration section.
    /// </summary>
    /// <typeparam name="TOptions">The plugin options type.</typeparam>
    /// <param name="plugin">The plugin whose name identifies the section.</param>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    public static void BindConfiguration<TOptions>(
        this IAuthKitPlugin plugin,
        IServiceCollection services,
        IConfiguration configuration)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<TOptions>(configuration.GetSection($"Plugins:{plugin.Name}"));
    }

    /// <summary>
    /// Checks if the plugin supports the specified capability.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="capability">The ability to check.</param>
    /// <returns><c>true</c> if the plugin supports the capability; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The comparison is case-insensitive.
    /// </remarks>
    public static bool Supports(this IAuthKitPlugin plugin, string capability) =>
        plugin == null
            ? throw new ArgumentNullException(nameof(plugin))
            : plugin.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the plugin manifest supports the specified capability.
    /// </summary>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="capability">The ability to check.</param>
    /// <returns><c>true</c> if the manifest supports the capability; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The comparison is case-insensitive.
    /// </remarks>
    public static bool Supports(this PluginManifest manifest, string capability) =>
        manifest == null
            ? throw new ArgumentNullException(nameof(manifest))
            : manifest.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
        
    /// <summary>
    /// Checks if the plugin has the specified dependency.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="dependencyId">The dependency ID to check.</param>
    /// <returns><c>true</c> if the plugin depends on the specified dependency; otherwise, <c>false</c>.</returns>
    public static bool HasDependency(this IAuthKitPlugin plugin, string dependencyId) =>
        plugin == null
            ? throw new ArgumentNullException(nameof(plugin))
            : plugin.DependsOn.Contains(dependencyId, StringComparer.OrdinalIgnoreCase);
        
    /// <summary>
    /// Checks if the plugin manifest has the specified dependency.
    /// </summary>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="dependencyId">The dependency ID to check.</param>
    /// <returns><c>true</c> if the manifest declares the specified dependency; otherwise, <c>false</c>.</returns>
    public static bool HasDependency(this PluginManifest manifest, string dependencyId) =>
        manifest == null
            ? throw new ArgumentNullException(nameof(manifest))
            : manifest.DependsOn.Contains(dependencyId, StringComparer.OrdinalIgnoreCase);
}