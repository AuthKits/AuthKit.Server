using System.Runtime.Loader;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.Discovery;

/// <summary>
/// A plugin whose assembly was loaded and whose <see cref="IAuthKitPlugin"/>
/// instance was constructed, but whose runtime behavior was not activated.
/// </summary>
/// <remarks>
/// Activation (endpoint mapping, middleware wiring, hosted services) is the
/// host's separate step after validation, consistency checks, and ordering.
/// </remarks>
public sealed record LoadedPlugin
{
    /// <summary>
    /// Manifest the plugin was discovered with. Required.
    /// </summary>
    public required PluginManifest Manifest { get; init; }

    /// <summary>
    /// Concrete plugin implementation type.
    /// </summary>
    public required Type PluginType { get; init; }

    /// <summary>
    /// Constructed plugin instance. Not activated.
    /// </summary>
    public required IAuthKitPlugin Instance { get; init; }

    /// <summary>
    /// Isolated load context the plugin was loaded into. Host and framework
    /// assemblies are shared; only plugin-private dependencies are isolated.
    /// Collectible to enable future unload/hot-reload orchestration.
    /// </summary>
    public required AssemblyLoadContext LoadContext { get; init; }
}
