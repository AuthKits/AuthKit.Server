namespace Shield.Detection;

/// <summary>
/// Normalized input for attack detectors: who is calling, what is targeted,
/// and when.
/// </summary>
public sealed record ShieldRequest(
    string ClientId,
    string Path,
    string Query,
    DateTimeOffset Timestamp,
    string UserAgent = "",
    long? ContentLength = null);

/// <summary>
/// What the host should do with a tripped signal.
/// </summary>
public enum ShieldAction
{
    /// <summary>
    /// Temporarily block the offending client; enough distinct offenders trigger global lockdown.
    /// </summary>
    Lockdown,

    /// <summary>
    /// Block only the offending request, without a global lockdown.
    /// </summary>
    BlockRequest,
}

/// <summary>
/// A detector verdict. Non-null means the detector tripped and the caller
/// should apply <see cref="Action"/> with <see cref="Reason"/>.
/// </summary>
public sealed record AttackSignal(string Sensor, string Reason, ShieldAction Action = ShieldAction.Lockdown);

/// <summary>
/// One attack-detection strategy. Implementations are pure policy: per-client
/// storage lives in <see cref="State.IShieldState"/> so HTTP and gRPC share it.
/// Detectors run in DI registration order; the first signal wins.
/// </summary>
public interface IAttackDetector
{
    /// <summary>
    /// Short sensor name used in incidents and diagnostics (e.g. "rate-flood").
    /// </summary>
    string Sensor { get; }

    /// <summary>
    /// Evaluates the request. Returns a signal when the request trips the
    /// detector, otherwise null.
    /// </summary>
    AttackSignal? Evaluate(ShieldRequest request, Options.ShieldOptions options);

    /// <summary>
    /// Observes the completed response (e.g. 401/403 counting). Returns a signal
    /// when the observation trips the detector; the client block then applies to
    /// subsequent requests. Only <see cref="ShieldAction.Lockdown"/> signals are
    /// honored here — the response is already sent. Default ignores responses.
    /// </summary>
    AttackSignal? ObserveResponse(ShieldRequest request, int statusCode, Options.ShieldOptions options) => null;
}
