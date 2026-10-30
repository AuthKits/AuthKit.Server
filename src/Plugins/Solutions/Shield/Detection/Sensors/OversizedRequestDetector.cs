namespace Shield.Detection.Sensors;

/// <summary>
/// Blocks (without a global lockdown) requests whose declared body or query
/// string exceeds the configured limits — the cheap-DoS / buffer-probe shape.
/// A single oversized request harms no one else, so it never trips a lockdown.
/// </summary>
public sealed class OversizedRequestDetector : IAttackDetector
{
    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "oversized-request";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options)
    {
        if (request.ContentLength is { } body && body > options.MaxBodyBytes)
            return new AttackSignal(
                Sensor,
                $"oversized body from {request.ClientId} ({body} bytes)",
                ShieldAction.BlockRequest);

        if (request.Query.Length > options.MaxQueryLength)
            return new AttackSignal(
                Sensor,
                $"oversized query from {request.ClientId} ({request.Query.Length} chars)",
                ShieldAction.BlockRequest);

        return null;
    }
}
