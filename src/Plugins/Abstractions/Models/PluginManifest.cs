using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;

namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Represents projection of plugin metadata available before plugin activation.
/// </summary>
/// <remarks>
/// <para>
/// The manifest exposes metadata required by the host to inspect, validate,
/// order, and prepare plugin before its runtime instance is activated.
/// </para>
/// <para>
/// The manifest mirrors selected runtime metadata exposed by
/// <see cref="IAuthKitPlugin"/> while remaining independent of a plugin instance.
/// This allows the host to perform pre-activation validation without loading
/// or activating the plugin.
/// </para>
/// <para>
/// Manifest metadata is also used after activation to validate consistency
/// between the declared manifest and the corresponding runtime plugin instance.
/// </para>
/// </remarks>
public sealed record PluginManifest
{
    /// <summary>
    /// Gets the unique identifier of the plugin.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the technical name of the plugin.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional user-facing display name of the plugin.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the optional description of the plugin.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the semantic version of the plugin.
    /// </summary>
    public SemanticVersion Version { get; init; } = new(0, 0, 0);

    /// <summary>
    /// Gets the optional author or publisher of the plugin.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets the optional license identifier or name associated with the plugin.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Gets the optional URL containing information about the plugin license.
    /// </summary>
    public string? LicenseUrl { get; init; }

    /// <summary>
    /// Gets the optional homepage URL of the plugin.
    /// </summary>
    public string? Homepage { get; init; }

    /// <summary>
    /// Gets the optional source repository URL of the plugin.
    /// </summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>
    /// Gets the optional classification tags associated with the plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tags are free form, case-sensitive strings intended for plugin
    /// classification, filtering, and discovery in user interfaces and
    /// plugin catalogs.
    /// </para>
    /// <para>
    /// The collection defaults to an empty set. Null, empty, or whitespace only
    /// elements are invalid and should be rejected during manifest validation.
    /// </para>
    /// <para>
    /// Example tags include <c>security</c>, <c>auth</c>, and <c>audit</c>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the priority used when ordering activation among dependency ready plugins.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lower priority values cause plugin to be activated earlier, while
    /// higher values cause it to be activated later.
    /// </para>
    /// <para>
    /// Priority is applied only among plugins whose dependencies are already
    /// satisfied. Dependency ordering therefore takes precedence over priority.
    /// </para>
    /// <para>
    /// The activation algorithm performs topological ordering and selects
    /// the dependency ready plugin with the lowest priority value.
    /// </para>
    /// </remarks>
    public int Priority { get; init; }

    /// <summary>
    /// Gets value indicating whether the plugin is enabled.
    /// </summary>
    /// <remarks>
    /// <para>The property defaults to <see langword="true"/>. </para>
    /// <para>
    /// A disabled plugin is skipped before loading and does not participate
    /// in plugin consistency validation.
    /// </para>
    /// <para>
    /// For plugins that pass the preload gate and are subsequently loaded,
    /// the manifest value is expected to match the corresponding runtime
    /// <c>IsEnabled</c> value.
    /// </para>
    /// </remarks>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the capabilities exposed by the plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capability identifiers are compared case-insensitively and default
    /// to an empty set.
    /// </para>
    /// <para>
    /// Capabilities are used for pre activation capability checks and for
    /// consistency validation after the plugin has been loaded.
    /// </para>
    /// <para>
    /// The <see cref="PluginExtensions.Supports(PluginManifest, string)"/>
    /// extension method can be used to determine whether the plugin declares
    /// a specific capability.
    /// </para>
    /// </remarks>
    public ISet<string> Capabilities { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the minimum host version required to load the plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A plugin is rejected during pre activation validation when the host
    /// version is lower than <see cref="MinHostVersion"/>.
    /// </para>
    /// <para>
    /// Version compatibility is evaluated using <see cref="SemanticVersion"/>
    /// comparison semantics.
    /// </para>
    /// </remarks>
    public SemanticVersion? MinHostVersion { get; init; }

    /// <summary>
    /// Gets the identifiers of plugins required by this plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each dependency must reference a valid plugin identifier discovered
    /// by the host.
    /// </para>
    /// <para>
    /// During validation, the host ensures that a plugin does not depend on
    /// itself, does not declare duplicate dependencies, and references only
    /// plugins that exist in the discovered plugin set.
    /// </para>
    /// <para>
    /// The dependency graph must also be acyclic. Dependency cycles prevent
    /// a valid activation order from being established.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> DependsOn { get; init; } = [];
}
