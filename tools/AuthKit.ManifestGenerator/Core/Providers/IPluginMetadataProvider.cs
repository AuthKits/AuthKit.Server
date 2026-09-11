using System;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.ManifestGenerator.Core.Providers;

/// <summary>
/// Defines the contract for retrieving plugin manifest metadata from plugin type.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for inspecting the specified plugin type
/// and constructing <see cref="PluginManifest"/> containing the metadata
/// exposed by the plugin.
/// </para>
/// <para>
/// The provider separates plugin metadata discovery from manifest generation,
/// allowing the discovered metadata to be consumed by different manifest
/// generation workflows.
/// </para>
/// </remarks>
public interface IPluginMetadataProvider
{
    /// <summary>
    /// Retrieves the manifest metadata associated with the specified plugin type.
    /// </summary>
    /// <param name="pluginType">
    /// The <see cref="Type"/> representing plugin from which manifest
    /// metadata is retrieved.
    /// </param>
    /// <returns>A <see cref="PluginManifest"/> containing metadata associated with specified plugin type.</returns>
    PluginManifest GetPluginManifest(Type pluginType);
}