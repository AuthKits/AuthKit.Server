namespace Host.Plugins.Loading.Results;

/// <summary>
/// One non loaded candidate with its reason, for startup diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Reason"/> is readable and log ready: it names the plugin
/// (when known) and the failing stage.
/// </para>
/// </remarks>
/// <param name="Location">The discovery location that was rejected or skipped.</param>
/// <param name="PluginId">The manifest ID, when the manifest was readable.</param>
/// <param name="Outcome">The terminal outcome for this candidate.</param>
/// <param name="Reason">Why the candidate did not load.</param>
public sealed record PluginLoadIssue(
    string Location,
    string? PluginId,
    PluginOutcome Outcome,
    string Reason);
