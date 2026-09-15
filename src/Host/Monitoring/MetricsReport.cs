namespace Host.Monitoring;

/// <summary>
/// Transport agnostic snapshot of basic process metrics.
/// </summary>
/// <remarks>
/// Shared by the REST /metrics endpoint and the gRPC
/// Monitoring.GetMetrics service.
/// </remarks>
/// <param name="UptimeSeconds">Seconds elapsed since the process started.</param>
/// <param name="ProcessStartUnixTime">Unix timestamp of the process start time.</param>
/// <param name="Time">The UTC time the report was produced.</param>
public sealed record MetricsReport(
    double UptimeSeconds,
    long ProcessStartUnixTime,
    DateTime Time);