using Shield.Options;

namespace Shield.State;

/// <summary>Combines the runtime gate with the configured manual kill-switch.</summary>
public static class ShieldStatus
{
    public static ShieldSnapshot GetSnapshot(IShieldState state, ShieldOptions options)
    {
        var snapshot = state.GetSnapshot();
        return options.ForceLockdown
            ? snapshot with { IsLocked = true, LockedUntil = null, Reason = "manual kill-switch" }
            : snapshot;
    }
}
