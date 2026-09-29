using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.Discovery;

/// <summary>
/// A plugin candidate found by an <see cref="IPluginDiscoverer"/>, before the
/// plugin assembly is loaded or activated.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Manifest"/> carries the declarative metadata read from
/// <c>plugin.json</c> / <c>manifest.json</c> during discovery, so the host can
/// run preload validation and the compatibility gate first. A manifest is
/// required: candidates without readable manifest are invalid and never load.
/// </para>
/// <para>
/// <see cref="DiscoveryError"/> is set when a manifest file was found but could
/// not be read or validated structurally; such candidates are invalid and must
/// never be loaded.
/// </para>
/// </remarks>
public sealed record DiscoveredPlugin
{
    /// <summary>
    /// Manifest read during discovery. Required; null only when
    /// <see cref="DiscoveryError"/> is set.
    /// </summary>
    public required PluginManifest? Manifest { get; init; }

    /// <summary>
    /// Source from which the loader can load the plugin. The format is
    /// discovery-source specific (directory, dll, package, uri, ...) and must
    /// not be interpreted by the contract.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Structural discovery failure (unreadable/invalid manifest). Null when
    /// discovery succeeded.
    /// </summary>
    public string? DiscoveryError { get; init; }
}
