namespace Shield.State.Counters;

/// <summary>Bounded per-client sliding-window counter with LRU eviction.</summary>
internal sealed class SlidingWindowCounter
{
    private const int PruneInterval = 1024;
    private readonly Lock _sync = new();
    private readonly Dictionary<string, ClientBucket> _buckets = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _recency = new();
    private int _recordsSincePrune;

    /// <summary>
    /// Records an event and returns the client's count inside the window.
    /// </summary>
    public int Record(string clientId, DateTimeOffset now, int windowSeconds, int maxTrackedClients)
    {
        lock (_sync)
        {
            var cutoff = now.AddSeconds(-windowSeconds);
            if (++_recordsSincePrune >= PruneInterval)
            {
                _recordsSincePrune = 0;
                PruneExpired(cutoff);
            }

            if (!_buckets.TryGetValue(clientId, out var bucket))
            {
                while (_buckets.Count >= maxTrackedClients)
                    Remove(_recency.First!.Value);

                bucket = new ClientBucket(_recency.AddLast(clientId));
                _buckets.Add(clientId, bucket);
            }
            else
            {
                _recency.Remove(bucket.RecencyNode);
                _recency.AddLast(bucket.RecencyNode);
            }

            while (bucket.Timestamps.Count > 0 && bucket.Timestamps.Peek() < cutoff)
                bucket.Timestamps.Dequeue();
            bucket.Timestamps.Enqueue(now);
            return bucket.Timestamps.Count;
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

    private void PruneExpired(DateTimeOffset cutoff)
    {
        foreach (var (clientId, bucket) in _buckets.ToArray())
        {
            while (bucket.Timestamps.Count > 0 && bucket.Timestamps.Peek() < cutoff)
                bucket.Timestamps.Dequeue();
            if (bucket.Timestamps.Count == 0)
                Remove(clientId);
        }
    }

    private void Remove(string clientId)
    {
        var bucket = _buckets[clientId];
        _recency.Remove(bucket.RecencyNode);
        _buckets.Remove(clientId);
    }

    private sealed class ClientBucket(LinkedListNode<string> recencyNode)
    {
        public LinkedListNode<string> RecencyNode { get; } = recencyNode;
        public Queue<DateTimeOffset> Timestamps { get; } = new();
    }
}
