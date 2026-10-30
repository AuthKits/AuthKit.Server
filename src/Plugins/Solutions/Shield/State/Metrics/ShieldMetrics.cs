namespace Shield.State.Metrics;

/// <summary>
/// Monotonic plugin metrics. Lock-free via <see cref="Interlocked"/>.
/// </summary>
internal sealed class ShieldMetrics
{
    private long _evaluated;
    private long _blocked;
    private long _trips;
    private long _authFailures;
    private long _probes;

    public void CountEvaluation() => Interlocked.Increment(ref _evaluated);
    public void CountBlock() => Interlocked.Increment(ref _blocked);
    public void CountTrip() => Interlocked.Increment(ref _trips);
    public void CountAuthFailure() => Interlocked.Increment(ref _authFailures);
    public void CountProbe() => Interlocked.Increment(ref _probes);

    public (long Evaluated, long Blocked, long Trips, long AuthFailures, long Probes) Read() => (
        Interlocked.Read(ref _evaluated),
        Interlocked.Read(ref _blocked),
        Interlocked.Read(ref _trips),
        Interlocked.Read(ref _authFailures),
        Interlocked.Read(ref _probes));
}
