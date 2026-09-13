namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Represents the operational health state reported by an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// The status is the authoritative health classification consumed by the host.
/// It must not be inferred from the optional reason or diagnostic data.
/// </para>
/// <para>
/// <see cref="Degraded"/> indicates that the plugin remains operational while
/// one or more non-critical capabilities or dependencies are impaired.
/// </para>
/// </remarks>
public enum PluginHealthStatus
{
    /// <summary>
    /// The plugin is operating normally.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The plugin remains operational, but one or more capabilities or
    /// non-critical dependencies are degraded.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The plugin cannot operate correctly, or a critical dependency has failed.
    /// </summary>
    Unhealthy = 2
}