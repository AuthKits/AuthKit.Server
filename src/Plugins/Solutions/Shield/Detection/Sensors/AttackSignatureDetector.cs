using Shield.State;

namespace Shield.Detection.Sensors;

/// <summary>
/// Trips when path or query contains known attack fragments (traversal,
/// SQL injection, XSS, suspicious system paths). Matching is case-insensitive
/// substring search over the raw (still-encoded) target.
/// </summary>
public sealed class AttackSignatureDetector(IShieldState state) : IAttackDetector
{
    // Lowercase fragments matched against path + query.
    private static readonly string[] Signatures =
    [
        "../", "..%2f", "..%5c", "%2e%2e",
        "union select", "union+select",
        "<script", "javascript:",
        "/etc/passwd", "xp_cmdshell",
        "' or '1'='1", "\" or \"1\"=\"1",
    ];

    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "attack-signature";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options)
    {
        if (!options.SignatureDetectionEnabled)
            return null;

        if (!ContainsSignature(request.Path + request.Query, options.ExtraSignatures))
            return null;

        var hits = state.RecordViolation(request.ClientId, request.Timestamp, options.SignatureWindowSeconds, options.MaxTrackedClients);
        return hits >= options.MaxSignatureHits
            ? new AttackSignal(Sensor, $"attack signatures from {request.ClientId} ({hits} hits)")
            : null;
    }

    internal static bool ContainsSignature(string target, string[]? extra = null)
    {
        if (Signatures.Any(signature => target.Contains(signature, StringComparison.OrdinalIgnoreCase)))
            return true;

        return extra?.Any(signature =>
            !string.IsNullOrWhiteSpace(signature) &&
            target.Contains(signature, StringComparison.OrdinalIgnoreCase)) == true;
    }
}
