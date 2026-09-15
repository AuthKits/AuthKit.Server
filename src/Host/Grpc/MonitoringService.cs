using AuthKit.Plugins.Abstractions.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Host.Monitoring;

namespace Host.Grpc;

/// <summary>
/// Exposes host and plugin health status and basic process metrics over gRPC.
/// </summary>
/// <remarks>
/// Mirrors the REST <c>/health</c> and <c>/metrics</c> endpoints
/// (<c>EndpointConfiguration</c>). Aggregation lives in
/// <see cref="HealthReportService"/> and <see cref="MetricsReportService"/>;
/// this service only maps the shared reports to the gRPC wire format.
/// </remarks>
public class MonitoringService(
    ILogger<MonitoringService> logger,
    HealthReportService healthReportService) : Monitoring.MonitoringBase
{
    /// <summary>
    /// Returns the aggregate health status of the JWT key store and every
    /// loaded plugin.
    /// </summary>
    public override async Task<MonitoringHealthResponse> GetHealth(Empty request, ServerCallContext context)
    {
        var report = await healthReportService.BuildAsync(context.CancellationToken);

        var response = new MonitoringHealthResponse
        {
            Status = report.Status,
            Time = report.Time.ToString("O"),
            JwtKeyStoreHealthy = report.JwtKeyStore == "Healthy"
        };

        foreach (var (name, results) in report.Plugins)
        {
            var pluginHealth = new PluginHealth { Name = name };
            foreach (var result in results)
            {
                var entry = new PluginHealthCheck
                {
                    Status = result.Status.ToString(),
                    Reason = result.Reason ?? string.Empty
                };

                if (result.Tags is not null)
                    entry.Tags.AddRange(result.Tags);

                if (result.Data is not null)
                {
                    foreach (var (key, value) in result.Data)
                        entry.Data[key] = value.ToString() ?? string.Empty;
                }

                pluginHealth.Results.Add(entry);
            }

            response.Plugins.Add(pluginHealth);
        }

        logger.LogDebug("gRPC health reported {Status} across {PluginCount} plugin(s).", response.Status, response.Plugins.Count);
        return response;
    }

    /// <summary>
    /// Returns basic process metrics such as uptime.
    /// </summary>
    public override Task<MetricsResponse> GetMetrics(Empty request, ServerCallContext context)
    {
        var report = MetricsReportService.Build();

        return Task.FromResult(new MetricsResponse
        {
            UptimeSeconds = report.UptimeSeconds,
            ProcessStartUnixTime = report.ProcessStartUnixTime,
            Time = report.Time.ToString("O")
        });
    }
}