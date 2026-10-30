namespace Shield.State.Incidents;

/// <summary>
/// Bounded incident ring buffer, newest first. Thread-safe.
/// </summary>
public sealed class IncidentLog
{
    private readonly Lock _sync = new();
    private readonly LinkedList<ShieldIncident> _incidents = new();

    /// <summary>
    /// Fired (under lock, keep handlers fast and non-blocking) for every
    /// added incident. Powers live feeds like gRPC streaming.
    /// </summary>
    public event Action<ShieldIncident>? Added;

    /// <summary>
    /// Prepends an incident, trimming to <paramref name="maxIncidents"/>.
    /// </summary>
    public void Add(ShieldIncident incident, int maxIncidents)
    {
        lock (_sync)
        {
            _incidents.AddFirst(incident);
            while (_incidents.Count > Math.Max(1, maxIncidents))
                _incidents.RemoveLast();

            Added?.Invoke(incident);
        }
    }

    /// <summary>
    /// Subscribes to new incidents. Dispose to unsubscribe.
    /// </summary>
    public IDisposable Subscribe(Action<ShieldIncident> handler)
    {
        lock (_sync)
            Added += handler;

        return new Subscription(this, handler);
    }

    /// <summary>
    /// Returns up to <paramref name="take"/> incidents, newest first.
    /// </summary>
    public IReadOnlyList<ShieldIncident> Take(int take)
    {
        lock (_sync)
            return _incidents.Take(Math.Max(1, take)).ToList();
    }

    private sealed class Subscription(IncidentLog log, Action<ShieldIncident> handler) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            lock (log._sync)
                log.Added -= handler;
        }
    }
}
