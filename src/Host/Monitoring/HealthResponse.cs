using AuthKit.Plugins.Abstractions.Models;

namespace Host.Monitoring;

/// <summary>
/// Transport agnostic representation of the host health response.
/// </summary>
/// <remarks>
/// Directly serializable to JSON by the REST /health endpoint.
/// Also consumed by the gRPC monitoring.GetHealth service, which
/// maps it to the protobuf wire format.
/// </remarks>
/// <param name="Status">Aggregate status string.</param>
/// <param name="Time">UTC timestamp of the report.</param>
/// <param name="JwtKeyStore">Key store status text.</param>
/// <param name="Plugins">Per plugin health results keyed by plugin name.</param>
public sealed record HealthResponse(
    string Status,
    DateTime Time,
    string JwtKeyStore,
    IReadOnlyDictionary<string, IReadOnlyCollection<PluginHealthResultResponse>> Plugins);

/// <summary>
/// Single structured health result for plugin.
/// </summary>
/// <remarks>
/// <see cref="PluginHealthResultResponse.Status"/> is serialized as the
/// numeric enum value (int) by the default System.Text.Json
/// configuration, matching the existing REST contract.
/// </remarks>
/// <param name="Status">Health status level.</param>
/// <param name="Reason">Readable explanation.</param>
/// <param name="Tags">Optional labels for observability.</param>
/// <param name="Data">Optional structured diagnostic data.</param>
public sealed record PluginHealthResultResponse(
    PluginHealthStatus Status,
    string? Reason,
    IReadOnlyCollection<string>? Tags,
    IReadOnlyDictionary<string, object>? Data);
