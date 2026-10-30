namespace Shield.State;

/// <summary>
/// Thread-safe singleton holding per-client counters, the lockdown state
/// machine, metrics, and a bounded incident log shared by the HTTP middleware
/// and the gRPC interceptor.
/// </summary>
public interface IShieldState
{
    /// <summary>
    /// Returns true while a lockdown is active, with its expiry and reason.
    /// Expired lockdowns are cleared lazily.
    /// </summary>
    bool IsLockdownActive(out DateTimeOffset lockedUntil, out string? reason);

    /// <summary>
    /// Enters lockdown for <paramref name="duration"/> and records an incident.
    /// Returns false when a lockdown is already active.
    /// </summary>
    bool TryEnterLockdown(string sensor, string reason, TimeSpan duration, int maxIncidents);

    /// <summary>Checks whether one client has an active temporary block.</summary>
    bool IsClientBlocked(string clientId, out DateTimeOffset blockedUntil, out string? reason);

    /// <summary>Blocks an offending client and enters global lockdown only after enough distinct offenders.</summary>
    void RecordOffender(string clientId, string sensor, string reason, TimeSpan duration,
        int maxTrackedClients, int minOffendingClientsForLockdown, int maxIncidents);

    /// <summary>
    /// Clears an active lockdown immediately and records an incident.
    /// </summary>
    void ReleaseLockdown(int maxIncidents);

    /// <summary>
    /// Records a request from <paramref name="clientId"/> and returns the number
    /// of that client's requests inside the sliding window.
    /// </summary>
    int RecordRequest(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients);

    /// <summary>
    /// Records an attack-signature hit from <paramref name="clientId"/> and
    /// returns the client's hit count inside the configured window.
    /// </summary>
    int RecordViolation(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients);

    /// <summary>
    /// Records a failed authentication (401/403) from <paramref name="clientId"/>
    /// and returns the client's count inside the sliding window.
    /// </summary>
    int RecordAuthFailure(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients);

    /// <summary>
    /// Records a probe (404) from <paramref name="clientId"/> and returns the
    /// client's count inside the sliding window.
    /// </summary>
    int RecordProbe(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients);

    /// <summary>
    /// Records a standalone incident without touching the lockdown state.
    /// </summary>
    void RecordIncident(string sensor, string reason, string action, int maxIncidents);

    /// <summary>
    /// Increments the evaluated-requests metric.
    /// </summary>
    void CountEvaluation();

    /// <summary>
    /// Increments the blocked-requests metric.
    /// </summary>
    void CountBlock();

    /// <summary>
    /// Returns up to <paramref name="take"/> most recent incidents, newest first.
    /// </summary>
    IReadOnlyList<ShieldIncident> GetIncidents(int take);

    /// <summary>
    /// Subscribes to new incidents for live feeds. Dispose to unsubscribe.
    /// Handlers run under the log lock: keep them fast and non-blocking.
    /// </summary>
    IDisposable SubscribeIncidents(Action<ShieldIncident> handler);

    /// <summary>
    /// Captures the current state for status endpoints and health checks.
    /// Best-effort point-in-time view across independently locked parts.
    /// </summary>
    ShieldSnapshot GetSnapshot();
}
