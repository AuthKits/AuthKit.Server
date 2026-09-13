using Marten;

namespace AuthKit.Plugins.Integrations;

/// <summary>
/// Allows a plugin to contribute optional Marten document-store configuration.
/// </summary>
public interface IMartenPlugin
{
    /// <summary>
    /// Configures the actual Marten store options used by the host.
    /// </summary>
    /// <param name="options">The host-owned Marten store options.</param>
    /// <remarks>
    /// <para>
    /// This optional hook is invoked once while the host builds its single Marten
    /// document store, before the store is opened. Plugins are processed in ascending
    /// plugin ID order, so invocation order is deterministic. It does not depend on
    /// discovery order.
    /// </para>
    /// <para>
    /// The hook configures <c>StoreOptions</c> only; it must not resolve
    /// <c>IDocumentStore</c> or <c>IDocumentSession</c>, which become available only
    /// after the service provider is built. Plugin services registered through
    /// <c>ConfigureServices</c> are already registered when this hook runs.
    /// </para>
    /// </remarks>
    void ConfigureMarten(StoreOptions options);
}