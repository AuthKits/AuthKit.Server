using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Represents a plugin assembly that has been successfully loaded and instantiated
/// by the validation host.
/// </summary>
/// <remarks>
/// The wrapper keeps both the activated <see cref="IAuthKitPlugin"/> instance and the
/// <see cref="Assembly"/> it was loaded from, so that individual contract rules can
/// inspect runtime metadata without re-loading the assembly.
/// </remarks>
/// <param name="instance">The activated plugin instance implementing <see cref="IAuthKitPlugin"/>.</param>
/// <param name="assembly">The assembly the plugin type was loaded from.</param>
public sealed class LoadedPlugin(IAuthKitPlugin instance, Assembly assembly)
{
    /// <summary>
    /// Gets the activated plugin instance implementing <see cref="IAuthKitPlugin"/>.
    /// </summary>
    public IAuthKitPlugin Instance { get; } = instance;

    /// <summary>
    /// Gets the assembly the plugin type was loaded from.
    /// </summary>
    public Assembly Assembly { get; } = assembly;
}
