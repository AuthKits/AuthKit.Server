using Microsoft.AspNetCore.Http;
using Shield.State;

namespace Shield.Detection.Sensors;

/// <summary>
/// Trips when a single client accumulates too many failed authentications
/// (401/403 responses) inside the window — the password-spraying / credential
/// stuffing shape. Observation happens after the endpoint runs, so the
/// client block applies to subsequent requests.
/// </summary>
public sealed class AuthBruteForceDetector(IShieldState state) : IAttackDetector
{
    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "auth-brute-force";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options) => null;

    /// <summary>
    /// Observes the completed response for this sensor policy.
    /// </summary>
    public AttackSignal? ObserveResponse(ShieldRequest request, int statusCode, Options.ShieldOptions options)
    {
        if (statusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
            return null;

        var count = state.RecordAuthFailure(request.ClientId, request.Timestamp, options.AuthFailureWindowSeconds, options.MaxTrackedClients);
        return count >= options.MaxAuthFailures
            ? new AttackSignal(Sensor, $"authentication failures from {request.ClientId} ({count} in {options.AuthFailureWindowSeconds}s)")
            : null;
    }
}
