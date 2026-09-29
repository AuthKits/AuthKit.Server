namespace AuthKit.Plugins.Abstractions.Contracts.Discovery;

/// <summary>
/// Constructs plugin instances from discovered candidates.
/// </summary>
/// <remarks>
/// The loader receives exactly what the discoverer produced: it does not
/// rediscover, does not decide compatibility (enabled/version/validation),
/// and does not activate runtime behavior. It loads assemblies, constructs
/// instances, and returns them for the host pipeline (validation, consistency,
/// ordering, activation).
/// </remarks>
/// <remarks>
/// Echo each candidate's manifest back on the returned entry. The pipeline
/// attributes results by manifest Id, so entries without a manifest cannot
/// be attributed and are ignored.
/// </remarks>
public interface IPluginLoader
{
    /// <summary>
    /// Loads and constructs the discovered plugins without activating them.
    /// </summary>
    Task<IReadOnlyList<LoadedPlugin>> LoadAsync(
        IReadOnlyList<DiscoveredPlugin> plugins,
        CancellationToken cancellationToken = default);
}
