using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.PluginContractValidator.Core;

/// <summary>
/// Loads plugin entry assembly and instantiates its <see cref="IAuthKitPlugin"/> implementation.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Loads the plugin entry assembly located at <paramref name="entryDll"/> and activates its
    /// <see cref="IAuthKitPlugin"/> implementation.
    /// </summary>
    /// <param name="entryDll">The full path to the plugin's entry assembly (named after its directory).</param>
    /// <returns>
    /// A <see cref="PluginLoadResult"/> describing the loaded plugin, or the errors encountered
    /// while loading it.
    /// </returns>
    PluginLoadResult Load(string entryDll);
}
