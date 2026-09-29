using System.Reflection;
using System.Runtime.Loader;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using ContractLoadedPlugin = AuthKit.Plugins.Abstractions.Contracts.Discovery.LoadedPlugin;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace Host.Plugins.Loading;

/// <summary>
/// Default loader resolves each discovered location to an entry assembly,
/// loads it, and constructs the plugin instance. Loads and constructs only
/// no compatibility decisions, no validation, no activation.
/// </summary>
/// <remarks>
/// <para>
/// Plugins load into <see cref="AssemblyLoadContext.Default"/> so shared
/// framework and package types resolve to the same runtime types on both sides
/// of the plugin boundary. No hot unloading.
/// </para>
/// <para>
/// Each plugin directory contributes at most one candidate: the entry assembly
/// whose file name matches the directory name. Anything else is logged and
/// skipped so the pipeline can report it as invalid.
/// </para>
/// </remarks>
/// <param name="logger">The logger used to report load results.</param>
public sealed class DefaultPluginLoader(ILogger logger) : IPluginLoader
{
    /// <summary>
    /// Loads assemblies and constructs one instance per discovered candidate.
    /// </summary>
    /// <param name="plugins">Discovered candidates, in pipeline order.</param>
    /// <param name="cancellationToken">Stops the load loop between candidates.</param>
    /// <returns>Constructed plugins, in input order. Candidates that fail to load are skipped.</returns>
    /// <remarks>
    /// Loads and constructs only: no compatibility decisions, no validation,
    /// no activation. Failures are logged with their location and skipped, so
    /// the pipeline can report them as invalid.
    /// </remarks>
    public Task<IReadOnlyList<ContractLoadedPlugin>> LoadAsync(
        IReadOnlyList<DiscoveredPlugin> plugins,
        CancellationToken cancellationToken = default)
    {
        var loaded = new List<ContractLoadedPlugin>();

        foreach (var discovered in plugins)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = TryLoad(discovered);
            if (result is not null)
                loaded.Add(result);
        }

        return Task.FromResult<IReadOnlyList<ContractLoadedPlugin>>(loaded);
    }

    private ContractLoadedPlugin? TryLoad(DiscoveredPlugin discovered)
    {
        if (discovered.Manifest is null)
        {
            logger.LogError(
                "Skipping plugin '{Location}': candidate has no manifest.",
                discovered.Location);
            return null;
        }

        var pluginDir = discovered.Location;
        var pluginName = Path.GetFileName(pluginDir);
        var entryDllPath = Path.Join(pluginDir, $"{pluginName}.dll");

        if (!File.Exists(entryDllPath))
        {
            logger.LogError(
                "Skipping plugin folder '{Dir}': expected entry assembly '{Dll}' not found.",
                pluginDir, entryDllPath);
            return null;
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
                return null;
            }

            var plugin = (IAuthKitPlugin)Activator.CreateInstance(pluginType)!;

            return new ContractLoadedPlugin
            {
                Manifest = discovered.Manifest,
                PluginType = pluginType,
                Instance = plugin,
            };
        }
        catch (Exception ex) when (ex is FileNotFoundException
            or FileLoadException
            or BadImageFormatException
            or ReflectionTypeLoadException
            or TypeLoadException
            or MissingMethodException
            or TargetInvocationException
            or InvalidCastException)
        {
            logger.LogError(ex, "Failed to load plugin from '{Dir}'.", pluginDir);
            return null;
        }
    }
}
