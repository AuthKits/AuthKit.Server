using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Loading.Results;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace Host.Plugins.Loading.Pipeline;

/// <summary>
/// Discovers and loads AuthKit plugins from a specified directory during host startup.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are loaded before <see cref="Microsoft.AspNetCore.Builder.WebApplicationBuilder.Build"/>
/// is called, allowing infrastructure such as Wolverine, Marten, and MVC to discover
/// plugin assemblies while their configuration is being built.
/// </para>
/// <para>
/// The pipeline is discovery -> preload validation -> compatibility gate ->
/// load -> contract validation -> consistency. Every plugin directory must carry
/// manifest less directories that are invalid. Custom
/// <see cref="IPluginDiscoverer"/> / <see cref="IPluginLoader"/> implementations
/// can replace the defaults.
/// </para>
/// <para>
/// This facade always returns the accepted plugins. Per candidate diagnostics
/// go to the log programmatic access uses <see cref="PluginLoadingPipeline"/>
/// with <see cref="PluginLoadResult"/> directly.
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
        LoadPlugins(pluginsRootPath, logger, hostVersion, discoverer: null, loader: null);

    /// <summary>
    /// Discovers and loads plugins with swappable discovery/loading.
    /// </summary>
    /// <param name="pluginsRootPath">Used only by the default discoverer ignored when <paramref name="discoverer"/> is supplied.</param>
    /// <param name="logger">The logger used to report plugin discovery, loading, and validation results. </param>
    /// <param name="hostVersion">The version of the host application, used to reject plugins that
    /// require a newer host.</param>
    /// <param name="discoverer">Custom discoverer. Defaults to directory discovery.</param>
    /// <param name="loader">Custom loader. Defaults to assembly loading.</param>
    /// <returns>A readonly collection containing all successfully loaded plugins.</returns>
    public static IReadOnlyList<LoadedPlugin> LoadPlugins(
        string pluginsRootPath,
        ILogger logger,
        SemanticVersion hostVersion,
        IPluginDiscoverer? discoverer,
        IPluginLoader? loader)
    {
        discoverer ??= new DirectoryPluginDiscoverer(pluginsRootPath, logger);
        loader ??= new DefaultPluginLoader(logger);

        var pipeline = new PluginLoadingPipeline(discoverer, loader, logger, hostVersion);
        var result = pipeline.RunAsync().GetAwaiter().GetResult();

        LogSummary(logger, result);
        return result.Loaded;
    }

    private static void LogSummary(ILogger logger, PluginLoadResult result)
    {
        foreach (var issue in result.Issues)
        {
            var message = issue.Outcome switch
            {
                PluginOutcome.SkippedDisabled => "Disabled",
                PluginOutcome.Rejected => "Rejected",
                _ => "Invalid",
            };

            logger.LogWarning(
                "Plugin {State}: '{Location}' ({Id}): {Reason}",
                message, issue.Location, issue.PluginId ?? "<unknown>", issue.Reason);
        }

        logger.LogInformation(
            "Plugin loading complete: {Loaded} loaded, {Issues} with issues.",
            result.Loaded.Count, result.Issues.Count);
    }
}
