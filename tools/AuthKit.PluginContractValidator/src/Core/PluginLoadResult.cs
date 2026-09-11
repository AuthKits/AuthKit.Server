using System.Collections.Generic;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Encapsulates the outcome of attempting to load a plugin assembly.
/// </summary>
/// <remarks>
/// When loading fails, <see cref="Plugin"/> is <c>null</c> and <see cref="Errors"/>
/// contains the human-readable reasons for the failure.
/// </remarks>
/// <param name="Plugin">The loaded plugin, or <c>null</c> when loading failed.</param>
/// <param name="Errors">The list of contract or loading violations encountered.</param>
public sealed record PluginLoadResult(LoadedPlugin? Plugin, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets a value indicating whether the plugin was loaded successfully.
    /// </summary>
    public bool Succeeded => Plugin is not null;
}
