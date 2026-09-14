using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Health;

/// <summary>
/// Configures host side plugin health execution and result caching.
/// </summary>
/// <remarks>
/// <para>
/// The cache stores only completed executions. A zero <see cref="CacheTtl"/>
/// disables caching, while positive value controls how long completed
/// result may be reused.
/// </para>
/// <para>
/// This option controls host observed execution behavior and does not alter the
/// plugin-owned diagnostic data in <see cref="PluginHealthResult"/>.
/// </para>
/// </remarks>
public sealed class PluginHealthExecutionOptions
{
    /// <summary>
    /// Gets or sets the cache lifetime for completed plugin health executions.
    /// A zero value disables caching. The default is 30 seconds.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(30);
}