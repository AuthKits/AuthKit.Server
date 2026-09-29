namespace Host.Plugins.Loading.Gate;

/// <summary>
/// Compatibility gate outcome for one manifest.
/// </summary>
/// <remarks>
/// <para>
/// The verdict is terminal per candidate <see cref="Accept"/> proceeds to
/// loading, while any other verdict reports its issue and skips all later
/// pipeline stages for that candidate.
/// </para>
/// </remarks>
internal enum GateVerdict
{
    /// <summary>Proceed to loading.</summary>
    Accept,

    /// <summary>Skip quietly disabled plugin.</summary>
    SkipDisabled,

    /// <summary>Hard reject incompatible host.</summary>
    Reject,
}
