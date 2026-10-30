namespace Shield.State.Lockdown;

/// <summary>
/// The lockdown state machine: expiry, reason, trip counting. Thread-safe.
/// </summary>
internal sealed class LockdownGate
{
    private readonly Lock _sync = new();
    private DateTimeOffset? _lockedUntil;
    private string? _reason;

    /// <summary>
    /// Returns true while a lockdown is active. Expired lockdowns are cleared lazily.
    /// </summary>
    public bool TryGetActive(out DateTimeOffset lockedUntil, out string? reason)
    {
        lock (_sync)
        {
            if (_lockedUntil is { } until && until > DateTimeOffset.UtcNow)
            {
                lockedUntil = until;
                reason = _reason;
                return true;
            }

            _lockedUntil = null;
            _reason = null;
            lockedUntil = default;
            reason = null;
            return false;
        }
    }

    /// <summary>
    /// Enters lockdown. Returns false when one is already active.
    /// </summary>
    public bool TryEnter(string reason, TimeSpan duration)
    {
        lock (_sync)
        {
            if (_lockedUntil is { } until && until > DateTimeOffset.UtcNow)
                return false;

            _lockedUntil = DateTimeOffset.UtcNow.Add(duration);
            _reason = reason;
            return true;
        }
    }

    /// <summary>
    /// Clears the lockdown. Returns false when none was active.
    /// </summary>
    public bool Release()
    {
        lock (_sync)
        {
            if (_lockedUntil is null)
                return false;

            _lockedUntil = null;
            _reason = null;
            return true;
        }
    }
}
