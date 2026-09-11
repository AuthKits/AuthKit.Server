using System.Collections.Immutable;
using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Represents an immutable runtime snapshot of plugin metadata.
/// </summary>
/// <remarks>
/// This record aggregates all metadata from <see cref="IAuthKitPlugin"/>
/// and <see cref="PluginManifest"/>. It is created after the plugin is loaded
/// and provides a consistent, immutable view of the plugin's metadata.
/// </remarks>
public sealed record PluginMetadata
{
    /// <summary>
    /// Gets the stable, host-unique identifier of the plugin.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the unique name of the plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the human-readable description of the plugin.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the version of the plugin as a semantic version (SemVer 2.0.0).
    /// </summary>
    public required SemanticVersion Version { get; init; }

    /// <summary>
    /// Optional author metadata.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Optional SPDX-style license string.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Optional absolute URI pointing to the license text.
    /// </summary>
    public string? LicenseUrl { get; init; }

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to a plugin homepage.
    /// </summary>
    public string? Homepage { get; init; }

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to the plugin repository.
    /// </summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>
    /// Optional classification tags for UI filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Priority used for activation ordering.
    /// </summary>
    public int Priority { get; init; }

    /// <summary>
    /// Indicates whether the plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Minimum host version required to load this plugin.
    /// </summary>
    public SemanticVersion? MinHostVersion { get; init; }

    /// <summary>
    /// List of plugin IDs this plugin depends on.
    /// </summary>
    public IReadOnlyList<string> DependsOn { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Features/capabilities exposed by the plugin.
    /// </summary>
    public IReadOnlySet<string> Capabilities { get; init; } = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional human-readable display name for UIs.
    /// </summary>
    public string? DisplayName { get; init; }
}