using Microsoft.AspNetCore.Http;
using Shield.State;

namespace Shield.Detection.Sensors;

/// <summary>
/// Trips when a single client collects too many 404s inside the window — the
/// directory-enumeration / vulnerability-probing shape
/// (<c>/.git/</c>, <c>/wp-admin/</c>, <c>/.env</c>). Observation happens after
/// the endpoint runs, so the lockdown applies to subsequent requests.
/// </summary>
public sealed class EndpointScannerDetector(IShieldState state) : IAttackDetector
{
    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "endpoint-scanner";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options) => null;

    /// <summary>
    /// Observes the completed response for this sensor policy.
    /// </summary>
    public AttackSignal? ObserveResponse(ShieldRequest request, int statusCode, Options.ShieldOptions options)
    {
        if (!options.ScannerDetectionEnabled || statusCode != StatusCodes.Status404NotFound)
            return null;

        var count = state.RecordProbe(request.ClientId, request.Timestamp, options.ScannerWindowSeconds, options.MaxTrackedClients);
        return count >= options.MaxNotFoundProbes
            ? new AttackSignal(Sensor, $"endpoint probing from {request.ClientId} ({count} missing paths in {options.ScannerWindowSeconds}s)")
            : null;
    }
}
