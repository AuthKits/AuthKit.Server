using System.Diagnostics;

namespace Host.Monitoring;

/// <summary>
/// Produces consistent process metrics for every transport.
/// </summary>
/// <remarks>
/// Returns the uptime and process start time of the current process so both
/// the REST and gRPC monitoring surfaces report the same values.
/// </remarks>
public static class MetricsReportService
{
    /// <summary>
    /// Builds the current process metrics report.
    /// </summary>
    public static MetricsReport Build()
    {
        var process = Process.GetCurrentProcess();
        var startedAt = DateTimeOffset.FromUnixTimeSeconds(
            new DateTimeOffset(process.StartTime.ToUniversalTime()).ToUnixTimeSeconds());

        return new MetricsReport(
            UptimeSeconds: (DateTime.UtcNow - process.StartTime).TotalSeconds,
            ProcessStartUnixTime: startedAt.ToUnixTimeSeconds(),
            Time: DateTime.UtcNow);
    }
}