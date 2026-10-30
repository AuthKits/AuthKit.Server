namespace Shield.Detection.Sensors;

/// <summary>
/// Trips immediately on known scanner/exploitation user agents (sqlmap, nuclei,
/// masscan, …). A single match is enough — legitimate clients never identify
/// as attack tooling.
/// </summary>
public sealed class SuspiciousUserAgentDetector : IAttackDetector
{
    private static readonly string[] DefaultAgents =
    [
        "sqlmap", "nikto", "masscan", "nessus", "nuclei",
        "dirbuster", "gobuster", "hydra", "metasploit", "wpscan",
    ];

    /// <summary>
    /// Short sensor name used in incidents and diagnostics.
    /// </summary>
    public string Sensor => "suspicious-agent";

    /// <summary>
    /// Evaluates the request against this sensor policy.
    /// </summary>
    public AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options)
    {
        if (!options.UserAgentDetectionEnabled)
            return null;

        var agent = request.UserAgent;
        if (string.IsNullOrWhiteSpace(agent))
            return null;

        var hit = DefaultAgents.Concat(options.ExtraBlockedAgents)
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .FirstOrDefault(entry => agent.Contains(entry, StringComparison.OrdinalIgnoreCase));

        return hit is null
            ? null
            : new AttackSignal(Sensor, $"scanner user-agent from {request.ClientId} ({hit.Trim()})");
    }
}
