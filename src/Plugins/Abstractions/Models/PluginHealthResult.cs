namespace AuthKit.Plugins.Abstractions.Models;

/// <summary>
/// Represents structured health observation reported by an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Status"/> provides the strongly typed health classification. The
/// optional <see cref="Reason"/> and <see cref="Data"/> members add context but
/// must not redefine or override that classification.
/// </para>
/// <para>
/// <see cref="Data"/> is owned by the plugin and may contain plugin-specific
/// diagnostic values such as dependency names, endpoint information, or queue
/// depth. The host may serialize this data without assigning it health semantics.
/// </para>
/// </remarks>
public sealed record PluginHealthResult
{
    /// <summary>
    /// Initializes a structured plugin health result.
    /// </summary>
    /// <param name="status">The strongly typed operational health state.</param>
    /// <param name="reason">An optional human-readable explanation of the health state.</param>
    /// <param name="data">Optional plugin-owned diagnostic data.</param>
    public PluginHealthResult(
        PluginHealthStatus status,
        string? reason = null,
        IReadOnlyDictionary<string, object>? data = null)
    {
        Status = status;
        Reason = reason;
        Data = data;
    }

    /// <summary>
    /// Gets the strongly typed operational health state.
    /// </summary>
    public PluginHealthStatus Status { get; init; }

    /// <summary>
    /// Gets the optional human-readable explanation of the health state.
    /// </summary>
    /// <remarks>
    /// A reason is not required when <see cref="Status"/> is
    /// <see cref="PluginHealthStatus.Healthy"/>.
    /// </remarks>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets optional plugin-owned diagnostic data.
    /// </summary>
    /// <remarks>
    /// Diagnostic data is extensible plugin-specific information. It must not
    /// be used to replace the strongly typed value of <see cref="Status"/>.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}