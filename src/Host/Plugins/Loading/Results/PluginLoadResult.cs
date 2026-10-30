namespace Host.Plugins.Loading.Results;

/// <summary>
/// Host internal loading outcome: accepted plugins plus per candidate issues.
/// For startup logs, diagnostics, health, and admin surfaces. Intentionally not
/// part of the plugin contract.
/// </summary>
/// <remarks>
/// <para>
/// Every discovered candidate appears exactly once: either in
/// <see cref="Loaded"/> or as one entry in <see cref="Issues"/>.
/// </para>
/// </remarks>
public sealed record PluginLoadResult(
    IReadOnlyList<LoadedPlugin> Loaded,
    IReadOnlyList<PluginLoadIssue> Issues);
