using Core.KeyManagement.Services;
using Host.Plugins.Loading;
using Wolverine;

namespace Host.Configuration.Infrastructure;

/// <summary>
/// Provides extension methods for registering Wolverine event handlers.
/// </summary>
/// <remarks>
/// <para>
/// Registers the Core assembly for Wolverine handler discovery and includes
/// assemblies contributed by dynamically loaded AuthKit plugins.
/// </para>
/// <para>
/// This allows plugin provided command and query handlers to be discovered
/// without requiring the Host project to reference plugin assemblies directly.
/// </para>
/// </remarks>
public static class EventHandlerRegistrationFactory
{
    /// <summary>
    /// Registers application and plugin assemblies for Wolverine handler discovery.
    /// </summary>
    /// <param name="opts">The Wolverine configuration options to modify.</param>
    /// <param name="plugins">
    /// The plugins loaded during application startup whose assemblies may
    /// contain Wolverine handlers.
    /// </param>
    public static void IncludeEventHandlers(
        this WolverineOptions opts,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        opts.Discovery.IncludeAssembly(typeof(JwtKeyStore).Assembly);
        foreach (var plugin in plugins)
            opts.Discovery.IncludeAssembly(plugin.Assembly);
    }
}
