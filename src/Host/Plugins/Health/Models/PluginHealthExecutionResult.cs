using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Health.Models;

/// <summary>
/// Represents plugin health execution together with host observed duration.
/// </summary>
/// <remarks>
/// <para>
/// The plugin results remain unchanged and preserve their status, reason, tags,
/// and diagnostic data. <see cref="Duration"/> is measured by the host and is
/// intentionally kept outside <see cref="PluginHealthResult.Data"/>.
/// </para>
/// <para>
/// Instances returned from the cache represent the duration of the original
/// completed execution, not the time spent serving the cached response.
/// </para>
/// </remarks>
/// <param name="Results">The structured results returned by the plugin.</param>
/// <param name="Duration">The monotonic host measured execution duration.</param>
public sealed record PluginHealthExecutionResult(
    IReadOnlyCollection<PluginHealthResult> Results,
    TimeSpan Duration);