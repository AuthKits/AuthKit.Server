using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using Host.Plugins.Loading.Manifest;

namespace Host.Plugins.Loading;

/// <summary>
/// Default discoverer one subdirectory per plugin under a root path.
/// Reads each manifest from the disk without loading any assembly, so the
/// compatibility gate can run first. Directories are never executed here.
/// </summary>
/// <remarks>
/// <para>
/// Candidates stream in ordinal directory order, including invalid ones:
/// unreadable manifests surface as candidates with <c>DiscoveryError</c> set
/// instead of throwing, so the pipeline can report every directory.
/// </para>
/// <para>
/// A missing root path is not an error discovery yields nothing, and the host
/// starts with zero plugins.
/// </para>
/// </remarks>
/// <param name="pluginsRootPath">The root directory containing one subdirectory per plugin.</param>
/// <param name="logger">The logger used to report discovery results.</param>
public sealed class DirectoryPluginDiscoverer(
    string pluginsRootPath,
    ILogger logger) : IPluginDiscoverer
{
    /// <summary>
    /// Streams one candidate per plugin directory, manifest included.
    /// </summary>
    /// <param name="cancellationToken">Stops discovery between directories.</param>
    /// <returns>Candidates in ordinal directory order, including invalid ones.</returns>
    /// <remarks>
    /// Never throws for a single bad directory: unreadable manifests surface as
    /// candidates with <c>DiscoveryError</c> set, so the pipeline can report them.
    /// </remarks>
    public async IAsyncEnumerable<DiscoveredPlugin> DiscoverAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(pluginsRootPath))
        {
            logger.LogWarning("Plugins path '{Path}' does not exist — starting with zero plugins.", pluginsRootPath);
            yield break;
        }

        foreach (var pluginDir in Directory.GetDirectories(pluginsRootPath).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PluginManifestReader.TryRead(pluginDir, out var manifest, out var error))
            {
                yield return new DiscoveredPlugin
                {
                    Manifest = null,
                    Location = pluginDir,
                    DiscoveryError = error,
                };
                continue;
            }

            yield return new DiscoveredPlugin
            {
                Manifest = manifest,
                Location = pluginDir,
            };
        }

        await Task.CompletedTask;
    }
}
