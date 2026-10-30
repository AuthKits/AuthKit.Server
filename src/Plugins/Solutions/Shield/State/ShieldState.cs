namespace Shield.State;

using Shield.State.Counters;
using Shield.State.Incidents;
using Shield.State.Lockdown;
using Shield.State.Metrics;

/// <summary>
/// Facade over the Shield state parts: lockdown gate, per-client counters,
/// metrics, and incident log. Each part owns its own synchronization; the
/// facade itself holds no locks.
/// </summary>
public sealed class ShieldState : IShieldState
{
    private const string RequestBucket = "requests";
    private const string ViolationBucket = "violations";
    private const string AuthFailureBucket = "auth-failures";
    private const string ProbeBucket = "probes";

    private readonly Lock _eventsSync = new();
    private readonly Dictionary<string, SlidingWindowCounter> _events = new(StringComparer.Ordinal);
    private readonly LockdownGate _lockdown = new();
    private readonly IncidentLog _incidents = new();
    private readonly ShieldMetrics _metrics = new();

    /// <summary>
    /// <summary>
    /// Asks the lockdown gate whether a lockdown is active.
    /// </summary>
    public bool IsLockdownActive(out DateTimeOffset lockedUntil, out string? reason) =>
        _lockdown.TryGetActive(out lockedUntil, out reason);

    /// <summary>
    /// <summary>
    /// Enters the lockdown gate and records the trip.
    /// </summary>
    public bool TryEnterLockdown(string sensor, string reason, TimeSpan duration, int maxIncidents)
    {
        if (!_lockdown.TryEnter(reason, duration))
            return false;

        _metrics.CountTrip();
        RecordIncident(sensor, reason, "lockdown-entered", maxIncidents);
        return true;
    }

    /// <summary>
    /// <summary>
    /// Releases the lockdown gate and records the release.
    /// </summary>
    public void ReleaseLockdown(int maxIncidents)
    {
        if (!_lockdown.Release())
            return;

        RecordIncident("operator", "manual release", "lockdown-released", maxIncidents);
    }

    /// <summary>
    /// <summary>
    /// Appends to the incident log.
    /// </summary>
    public void RecordIncident(string sensor, string reason, string action, int maxIncidents) =>
        _incidents.Add(new ShieldIncident(DateTimeOffset.UtcNow, sensor, reason, action), maxIncidents);

    /// <summary>
    /// <summary>
    /// Counts the request in the request window.
    /// </summary>
    public int RecordRequest(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients) =>
        Event(RequestBucket).Record(clientId, now, windowSeconds, maxTrackedClients);

    /// <summary>
    /// <summary>
    /// Counts the hit in the violation counter.
    /// </summary>
    public int RecordViolation(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients) =>
        Event(ViolationBucket).Record(clientId, now, windowSeconds, maxTrackedClients);

    /// <summary>
    /// <summary>
    /// Counts the failure in the auth-failure window.
    /// </summary>
    public int RecordAuthFailure(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients)
    {
        _metrics.CountAuthFailure();
        return Event(AuthFailureBucket).Record(clientId, now, windowSeconds, maxTrackedClients);
    }

    /// <summary>
    /// <summary>
    /// Counts the probe in the probe window.
    /// </summary>
    public int RecordProbe(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients)
    {
        _metrics.CountProbe();
        return Event(ProbeBucket).Record(clientId, now, windowSeconds, maxTrackedClients);
    }

    /// <summary>
    /// <summary>
    /// Bumps the evaluated-requests metric.
    /// </summary>
    public void CountEvaluation() => _metrics.CountEvaluation();

    /// <summary>
    /// <summary>
    /// Bumps the blocked-requests metric.
    /// </summary>
    public void CountBlock() => _metrics.CountBlock();

    /// <summary>
    /// <summary>
    /// Reads the incident log.
    /// </summary>
    public IReadOnlyList<ShieldIncident> GetIncidents(int take) => _incidents.Take(take);

    /// <summary>
    /// <summary>
    /// Subscribes to the incident log.
    /// </summary>
    public IDisposable SubscribeIncidents(Action<ShieldIncident> handler) => _incidents.Subscribe(handler);

    /// <summary>
    /// <summary>
    /// Composes a snapshot from the state parts.
    /// </summary>
    public ShieldSnapshot GetSnapshot()
    {
        var locked = _lockdown.TryGetActive(out var lockedUntil, out var reason);
        var (evaluated, blocked, trips, authFailures, probes) = _metrics.Read();
        return new ShieldSnapshot(
            locked,
            locked ? lockedUntil : null,
            locked ? reason : null,
            EventClientCount(RequestBucket),
            evaluated,
            blocked,
            trips,
            authFailures,
            probes);
    }

    private SlidingWindowCounter Event(string bucket)
    {
        lock (_eventsSync)
        {
            if (!_events.TryGetValue(bucket, out var counter))
            {
                counter = new SlidingWindowCounter();
                _events[bucket] = counter;
            }

            return counter;
        }
    }

    private int EventClientCount(string bucket)
    {
        lock (_eventsSync)
            return _events.TryGetValue(bucket, out var counter) ? counter.ClientCount : 0;
    }
}
