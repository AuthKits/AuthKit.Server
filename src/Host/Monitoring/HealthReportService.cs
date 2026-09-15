using AuthKit.Plugins.Abstractions.Models;
using Core.KeyManagement.Interfaces;
using Host.Plugins.Health;
using Host.Plugins.Loading;

namespace Host.Monitoring;

/// <summary>
/// Builds consistent host and plugin health reports for every transport.
/// </summary>
/// <remarks>
/// Aggregates the health of the JWT key store and every loaded plugin using
/// the key store and plugin health infrastructure shared with the REST
/// endpoint. The result is transport agnostic and is serialized by the REST
/// /health endpoint and the gRPC Monitoring.GetHealth service.
/// </remarks>
public sealed class HealthReportService(
    IJwtKeyStore keyStore,
    IReadOnlyList<LoadedPlugin> plugins,
    PluginHealthExecutor healthExecutor)
{
    /// <summary>
    /// Builds transport agnostic health response that can be serialized
    /// to JSON by the REST endpoint and mapped to protobuf by the gRPC
    /// Monitoring service.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the plugin health checks.</param>
    public async Task<HealthResponse> BuildAsync(CancellationToken cancellationToken = default)
    {
        var keyStoreHealthy = keyStore.GetPublicJwks().Any();

        var pluginResults = new Dictionary<string, IReadOnlyCollection<PluginHealthResultResponse>>();
        foreach (var plugin in plugins)
        {
            var results = (await healthExecutor.ExecuteAsync(plugin, cancellationToken))
                .Results
                .Select(r => new PluginHealthResultResponse(
                    r.Status,
                    r.Reason,
                    r.Tags,
                    r.Data))
                .ToArray();

            pluginResults[plugin.Plugin.Name] = results;
        }

        var aggregated = pluginResults.Values
            .SelectMany(results => results)
            .Select(r => r.Status)
            .DefaultIfEmpty(PluginHealthStatus.Healthy)
            .Max();
        var status = keyStoreHealthy ? aggregated : PluginHealthStatus.Unhealthy;

        return new HealthResponse(
            status.ToString(),
            DateTime.UtcNow,
            keyStoreHealthy ? "Healthy" : "Unhealthy",
            pluginResults);
    }
}