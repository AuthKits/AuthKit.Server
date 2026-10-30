namespace Shield.State;

/// <summary>
/// One recorded security event: lockdown entered/released.
/// </summary>
public sealed record ShieldIncident(
    DateTimeOffset Timestamp,
    string Sensor,
    string Reason,
    string Action);

/// <summary>
/// Point-in-time view of the Shield lockdown state for status and health reporting.
/// </summary>
public sealed record ShieldSnapshot(
    bool IsLocked,
    DateTimeOffset? LockedUntil,
    string? Reason,
    int TrackedClients,
    long TotalEvaluated,
    long TotalBlocked,
    long TotalTrips,
    long TotalAuthFailures,
    long TotalProbes);
