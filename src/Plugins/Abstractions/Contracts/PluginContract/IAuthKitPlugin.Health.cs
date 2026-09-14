using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Performs an optional structured health check for the plugin.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <param name="cancellationToken">A token that can cancel the health check.</param>
    /// <returns>
    /// One or more structured health results reported by the plugin.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The host may invoke this method as part of its health endpoint.
    /// Plugins can resolve the services they require from
    /// <paramref name="services"/> to verify the availability of their
    /// dependencies.
    /// </para>
    /// <para>
    /// A plugin may return separate results for independent dependencies or
    /// capabilities. Cancellation must be propagated to cancellable operations
    /// and is not converted into a fabricated health result.
    /// </para>
    /// <para>
    /// The default implementation reports the plugin as healthy. Existing
    /// plugins that do not require custom health validation therefore do not
    /// need to implement this member.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PluginHealthResult>>(
        [new PluginHealthResult(PluginHealthStatus.Healthy)]);
}