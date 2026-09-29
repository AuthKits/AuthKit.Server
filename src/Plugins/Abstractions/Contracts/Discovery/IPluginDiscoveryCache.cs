namespace AuthKit.Plugins.Abstractions.Contracts.Discovery;

/// <summary>
/// Host owned discovery cache sitting before <see cref="IPluginDiscoverer"/>.
/// </summary>
/// <remarks>
/// A hit reuses cached <see cref="DiscoveredPlugin"/> entries without calling
/// the discoverer. Only discovery metadata is cached — never load contexts,
/// types, or instances.
/// </remarks>
public interface IPluginDiscoveryCache
{
    /// <summary>
    /// Returns cached discovery results or null on cache miss or staleness.
    /// </summary>
    Task<IReadOnlyList<DiscoveredPlugin>?> TryGetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores fresh discovery results for future hits.
    /// </summary>
    Task StoreAsync(IReadOnlyList<DiscoveredPlugin> plugins, CancellationToken cancellationToken = default);
}
