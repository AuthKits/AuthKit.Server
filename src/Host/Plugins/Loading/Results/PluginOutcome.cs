namespace Host.Plugins.Loading.Results;

/// <summary>
/// Outcome of one discovered candidate in the loading pipeline.
/// </summary>
/// <remarks>
/// Outcomes are terminal per candidate: the first failing pipeline stage
/// reports its issue, and later stages never run for that candidate.
/// </remarks>
public enum PluginOutcome
{
    /// <summary>Loaded and accepted.</summary>
    Loaded,

    /// <summary>Skipped before loading: disabled plugin.</summary>
    SkippedDisabled,

    /// <summary>Hard reject: incompatible host or contract violation.</summary>
    Rejected,

    /// <summary>Invalid: unreadable manifest, structural errors, duplicate Id, or load failure.</summary>
    Invalid,
}
