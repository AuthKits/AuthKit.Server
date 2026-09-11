using System.Collections.Immutable;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Provides extension methods for working with plugin metadata.
/// </summary>
public static class PluginMetadataExtensions
{
    /// <summary>
    /// Creates an immutable snapshot of the plugin's metadata.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    /// <returns>An immutable <see cref="PluginMetadata"/> record.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the plugin is null.</exception>
    public static PluginMetadata GetMetadata(this IAuthKitPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        return new PluginMetadata
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Description = plugin.Description ?? string.Empty,
            Version = plugin.Version,
            Author = plugin.Author,
            License = plugin.License,
            LicenseUrl = plugin.LicenseUrl,
            Homepage = plugin.Homepage,
            RepositoryUrl = plugin.RepositoryUrl,
            Tags = plugin.Tags.ToArray(),
            Priority = plugin.Priority,
            IsEnabled = plugin.IsEnabled,
            MinHostVersion = plugin.MinHostVersion,
            DependsOn = plugin.DependsOn.ToArray(),
            Capabilities = plugin.Capabilities.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
            DisplayName = plugin.DisplayName
        };
    }
}