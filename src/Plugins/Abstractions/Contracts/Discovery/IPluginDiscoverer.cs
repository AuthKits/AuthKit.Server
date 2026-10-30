namespace AuthKit.Plugins.Abstractions.Contracts.Discovery;

/// <summary>
/// Finds candidate plugins and reads their manifests without activating them.
/// </summary>
/// <remarks>
/// Discovery MUST NOT activate plugins: no assembly loading for execution, no
/// instance construction, no runtime behavior. All compatibility decisions
/// (enabled, version gate, validation) happen in the host pipeline around the
/// loader, never inside discovery.
/// </remarks>
public interface IPluginDiscoverer
{
    /// <summary>
    /// Streams discovered plugin candidates, manifest included.
    /// </summary>
    IAsyncEnumerable<DiscoveredPlugin> DiscoverAsync(CancellationToken cancellationToken = default);
}
