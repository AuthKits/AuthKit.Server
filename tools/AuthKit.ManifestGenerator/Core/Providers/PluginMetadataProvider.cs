using System;
using System.Collections.Immutable;
using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.ManifestGenerator.Core.Providers;

/// <summary>
/// Provides plugin manifest metadata by inspecting plugin type decorated
/// with <see cref="PluginMetadataAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The provider reads metadata declared through <see cref="PluginMetadataAttribute"/>
/// and maps it to <see cref="PluginManifest"/> instance used by the manifest
/// generation pipeline.
/// </para>
/// <para>
/// Semantic versions declared by the plugin are parsed into
/// <see cref="SemanticVersion"/> instances. Optional host version requirements
/// are parsed when valid value is provided.
/// </para>
/// </remarks>
public class PluginMetadataProvider : IPluginMetadataProvider
{
    /// <summary>
    /// Retrieves the manifest metadata associated with the specified plugin type.
    /// </summary>
    /// <param name="pluginType">The <see cref="Type"/> representing the plugin from which metadata is retrieved.</param>
    /// <returns>A <see cref="PluginManifest"/>containing the metadata declared by the plugin.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pluginType"/> is <see langword="null"/>. </exception>
    /// <exception cref="InvalidOperationException">Thrown when the specified plugin type is not decorated with <see cref="PluginMetadataAttribute"/>. </exception>
    public PluginManifest GetPluginManifest(Type pluginType)
    {
        if (pluginType == null)
        {
            throw new ArgumentNullException(nameof(pluginType));
        }

        var attribute = pluginType.GetCustomAttribute<PluginMetadataAttribute>();

        if (attribute == null)
        {
            throw new InvalidOperationException(
                "PluginMetadataAttribute not found on plugin type. " +
                "Ensure the plugin class is decorated with [PluginMetadata].");
        }

        return MapToPluginManifest(attribute);
    }

    /// <summary>
    /// Maps plugin metadata declared by <see cref="PluginMetadataAttribute"/>
    /// to <see cref="PluginManifest"/> instance.
    /// </summary>
    /// <param name="attribute">The <see cref="PluginMetadataAttribute"/> containing the plugin metadata. </param>
    /// <returns>A <see cref="PluginManifest"/>Populated with metadata declared by specified attribute. </returns>
    /// <exception cref="ArgumentException">Thrown when the plugin version is not a valid semantic version. </exception>
    private static PluginManifest MapToPluginManifest(PluginMetadataAttribute attribute)
    {
        if (!SemanticVersion.TryParse(attribute.Version, out var version))
        {
            throw new ArgumentException(
                $"Version '{attribute.Version}' is not a valid semantic version.");
        }

        SemanticVersion? minHostVersion = null;

        if (!string.IsNullOrEmpty(attribute.MinHostVersion) &&
            SemanticVersion.TryParse(attribute.MinHostVersion, out var parsedMinHost))
        {
            minHostVersion = parsedMinHost;
        }

        return new PluginManifest
        {
            Id = attribute.Id,
            Name = attribute.Name,
            DisplayName = attribute.DisplayName,
            Description = attribute.Description,
            Version = version,
            Author = attribute.Author ?? string.Empty,
            License = attribute.License ?? string.Empty,
            LicenseUrl = attribute.LicenseUrl ?? string.Empty,
            Homepage = attribute.Homepage ?? string.Empty,
            RepositoryUrl = attribute.RepositoryUrl ?? string.Empty,
            Tags = attribute.Tags,
            DependsOn = attribute.DependsOn,
            Capabilities = attribute.Capabilities.ToImmutableHashSet(
                StringComparer.OrdinalIgnoreCase),
            Priority = attribute.Priority,
            IsEnabled = attribute.IsEnabled,
            MinHostVersion = minHostVersion
        };
    }
}