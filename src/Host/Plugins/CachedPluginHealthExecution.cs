using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins;

/// <summary>
/// Stores a completed plugin health execution until its cache expiry time.
/// </summary>
/// <remarks>
/// <para>
/// This host owned cache entry keeps execution duration separate from the
/// plugin owned diagnostic data. It is never created for an incomplete or
/// cancelled execution.
/// </para>
/// <para>
/// The cache key is the stable plugin identifier, so results cannot be reused
/// across different plugins.
/// </para>
/// </remarks>
/// <param name="Results">The structured results returned by the plugin.</param>
/// <param name="Duration">The duration measured during the completed execution.</param>
/// <param name="ExpiresAt">The UTC time after which this entry is invalid.</param>
internal sealed record CachedPluginHealthExecution(
    IReadOnlyCollection<PluginHealthResult> Results,
    TimeSpan Duration,
    DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Converts the cached entry to the public host execution result.
    /// </summary>
    public PluginHealthExecutionResult ToExecutionResult() =>
        new(Results, Duration);
}