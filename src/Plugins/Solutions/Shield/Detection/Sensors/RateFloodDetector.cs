using Shield.State;

namespace Shield.Detection.Sensors;

/// <summary>
/// Trips when a single client exceeds the configured request rate inside the
/// sliding window.
/// </summary>
public sealed class RateFloodDetector(IShieldState state) : IAttackDetector
{
    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "rate-flood";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options)
    {
        var count = state.RecordRequest(request.ClientId, request.Timestamp, options.WindowSeconds, options.MaxTrackedClients);
        return count > options.MaxRequestsPerWindow
            ? new AttackSignal(Sensor, $"request flood from {request.ClientId} ({count} requests in {options.WindowSeconds}s)")
            : null;
    }
}
