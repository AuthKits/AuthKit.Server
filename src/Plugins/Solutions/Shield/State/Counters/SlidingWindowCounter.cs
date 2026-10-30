namespace Shield.State.Counters;

/// <summary>
/// Per-client sliding-window event counter (request rate, auth failures).
/// Thread-safe.
/// </summary>
internal sealed class SlidingWindowCounter
{
    private readonly Lock _sync = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _buckets = new(StringComparer.Ordinal);

    /// <summary>
    /// Records an event and returns the client's count inside the window.
    /// </summary>
    public int Record(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients)
    {
        lock (_sync)
        {
            var cutoff = now.AddSeconds(-windowSeconds);
            if (!_buckets.ContainsKey(clientId) && _buckets.Count >= maxTrackedClients)
            {
                foreach (var (id, events) in _buckets.ToArray())
                {
                    while (events.Count > 0 && events.Peek() < cutoff)
                        events.Dequeue();
                    if (events.Count == 0)
                        _buckets.Remove(id);
                }

                while (_buckets.Count >= maxTrackedClients)
                {
                    var oldest = _buckets.MinBy(entry => entry.Value.Peek().UtcTicks).Key;
                    _buckets.Remove(oldest);
                }
            }

            if (!_buckets.TryGetValue(clientId, out var timestamps))
            {
                timestamps = new Queue<DateTimeOffset>();
                _buckets[clientId] = timestamps;
            }

            while (timestamps.Count > 0 && timestamps.Peek() < cutoff)
                timestamps.Dequeue();
            timestamps.Enqueue(now);
            return timestamps.Count;
        }
    }

    /// <summary>
    /// Number of retained clients (including entries not yet pruned after expiry).
    /// </summary>
    public int ClientCount
    {
        get
        {
            lock (_sync)
                return _buckets.Count;
        }
    }
}
