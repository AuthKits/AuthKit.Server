using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Gets the minimum host version required to load this plugin.
    /// </summary>
    /// <remarks>
    /// If the host version is lower than <see cref="MinHostVersion"/>, the plugin is rejected.
    /// </remarks>
    SemanticVersion? MinHostVersion => string.IsNullOrEmpty(Metadata.MinHostVersion) ? null : SemanticVersion.Parse(Metadata.MinHostVersion);

    /// <summary>
    /// Gets the list of plugin IDs this plugin depends on.
    /// </summary>
    /// <remarks>
    /// Each entry must be a valid plugin ID. The host validates that:
    /// - Dependencies exist among discovered plugins.
    /// - There are no self-dependencies.
    /// - There are no duplicate dependencies.
    /// - There are no dependency cycles.
    /// </remarks>
    IReadOnlyList<string> DependsOn => Metadata.DependsOn;
}