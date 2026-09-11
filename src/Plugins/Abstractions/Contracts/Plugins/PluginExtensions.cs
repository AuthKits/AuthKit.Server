using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Extension methods for plugin capabilities and metadata.
/// </summary>
public static class PluginExtensions
{
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