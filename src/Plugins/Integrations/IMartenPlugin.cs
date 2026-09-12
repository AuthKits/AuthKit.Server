using Marten;

namespace AuthKit.Plugins.Integrations;

/// <summary>
/// Allows a plugin to contribute optional Marten document-store configuration.
/// </summary>
public interface IMartenPlugin
{
    /// <summary>Configures the actual Marten store options used by the host.</summary>
    /// <param name="options">The host-owned Marten store options.</param>
    void ConfigureMarten(StoreOptions options);
}