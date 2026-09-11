using System.Runtime.Loader;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Host.Plugins;

/// <summary>
/// Discovers and loads AuthKit plugins from specified directory during host startup.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are loaded before <see cref="Microsoft.AspNetCore.Builder.WebApplicationBuilder.Build"/>
/// is called, allowing infrastructure such as Wolverine, Marten, and MVC to discover
/// plugin assemblies while their configuration is being built.
/// </para>
/// <para>
/// Plugins are loaded into <see cref="AssemblyLoadContext.Default"/> rather than an
/// isolated load context. This ensures that shared framework and package types, such
/// as Wolverine <c>IMessageBus</c>, Marten <c>IDocumentSession</c>, and ASP.NET Core
/// MVC types, resolve to the same runtime types on both sides of the plugin boundary.
/// </para>
/// <para>
/// The loader does not support hot unloading or hot swapping of plugins. Plugins are
/// expected to remain loaded for the lifetime of the host process.
/// </para>
/// </remarks>
public static class PluginLoader
{
    /// <summary>
    /// Discovers and loads all valid AuthKit plugins from the specified root directory.
    /// </summary>
    /// <param name="pluginsRootPath">The root directory containing one subdirectory per plugin. </param>
    /// <param name="logger">The logger used to report plugin discovery, loading, and validation results. </param>
    /// <param name="hostVersion">The version of the host application, used to reject plugins that
    /// require a newer host.</param>
    /// <returns>A readonly collection containing all successfully loaded plugins.</returns>
    /// <remarks>
    /// Each plugin directory is expected to contain an entry assembly whose file name
    /// matches the directory name. Directories without matching assembly or assemblies
    /// without valid <see cref="IAuthKitPlugin"/> implementation are skipped.
    /// </remarks>
    public static IReadOnlyList<LoadedPlugin> LoadPlugins(
        string pluginsRootPath,
        ILogger logger,
        SemanticVersion hostVersion) =>
        LoadPluginsCore(pluginsRootPath, logger, hostVersion);

    /// <summary>
    /// Discovers and loads all valid AuthKit plugins from the specified root directory.
    /// </summary>
    /// <param name="pluginsRootPath">The root directory containing one subdirectory per plugin. </param>
    /// <param name="logger">The logger used to report plugin discovery, loading, and validation results. </param>
    /// <returns>A readonly collection containing all successfully loaded plugins.</returns>
    [Obsolete("Use the overload taking a host version to enforce plugin MinHostVersion compatibility.")]
    public static IReadOnlyList<LoadedPlugin> LoadPlugins(
        string pluginsRootPath,
        ILogger logger) =>
        LoadPluginsCore(pluginsRootPath, logger, hostVersion: null);

    private static IReadOnlyList<LoadedPlugin> LoadPluginsCore(
        string pluginsRootPath,
        ILogger logger,
        SemanticVersion? hostVersion)
    {
        if (!Directory.Exists(pluginsRootPath))
        {
            logger.LogWarning("Plugins path '{Path}' does not exist — starting with zero plugins.", pluginsRootPath);
            return [];
        }

        var loaded = new List<LoadedPlugin>();

        foreach (var pluginDir in Directory.GetDirectories(pluginsRootPath))
        {
            var pluginName = Path.GetFileName(pluginDir);
            var entryDllPath = Path.Join(pluginDir, $"{pluginName}.dll");

            if (!File.Exists(entryDllPath))
            {
                logger.LogError(
                    "Skipping plugin folder '{Dir}': expected entry assembly '{Dll}' not found.",
                    pluginDir, entryDllPath);
                continue;
            }

            try
            {
                var resolver = new AssemblyDependencyResolver(entryDllPath);
                AssemblyLoadContext.Default.Resolving += (context, name) =>
                {
                    var path = resolver.ResolveAssemblyToPath(name);
                    return path is not null ? context.LoadFromAssemblyPath(path) : null;
                };

                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(entryDllPath);

                var pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => t is { IsPublic: true, IsAbstract: false }
                                          && typeof(IAuthKitPlugin).IsAssignableFrom(t)
                                          && t.GetConstructor(Type.EmptyTypes) is not null);

                if (pluginType is null)
                {
                    logger.LogError(
                        "Skipping plugin assembly '{Dll}': no public, non-abstract IAuthKitPlugin implementation with a parameterless constructor found.",
                        entryDllPath);
                    continue;
                }

                var plugin = (IAuthKitPlugin)Activator.CreateInstance(pluginType)!;

                if (hostVersion is { } hv && plugin.MinHostVersion is { } minVersion && hv < minVersion)
                {
                    logger.LogError(
                        "Skipping plugin '{Dir}': host version {HostVersion} is lower than required minimum {MinVersion}.",
                        pluginDir, hv, minVersion);
                    continue;
                }

                // Validate plugin contract against host capabilities
                PluginContractValidator.Validate(plugin, logger);

                loaded.Add(new LoadedPlugin(plugin, assembly, pluginDir));

                logger.LogInformation("Loaded plugin '{Name}' v{Version} from {Dir}", plugin.Name, plugin.Version, pluginDir);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load plugin from '{Dir}'.", pluginDir);
            }
        }

        return loaded;
    }
}
