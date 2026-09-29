using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Loading.Gate;

/// <summary>
/// Preload compatibility gate. Runs on the manifest before the plugin assembly
/// is loaded. The first version rule is <c>MinHostVersion</c>. Rejections are
/// hard failures, never warn only loading a plugin that requires a newer host
/// risks <c>MissingMethodException</c> / <c>TypeLoadException</c>.
/// </summary>
/// <remarks>
/// <para>
/// The gate inspects the manifest only. It never loads assemblies, constructs
/// instances, or activates runtime behavior, so rejected plugins cost nothing
/// beyond manifest parsing.
/// </para>
/// <para>
/// Version policy may grow here (capabilities, platform, ceilings) without
/// touching discovery or loading.
/// </para>
/// </remarks>
internal static class CompatibilityGate
{
    /// <summary>
    /// Evaluates the manifest against the host version.
    /// </summary>
    /// <param name="manifest">The discovered manifest.</param>
    /// <param name="hostVersion">The running host version. Null skips the version rule.</param>
    /// <returns>The verdict with a human-readable reason for non-accepts.</returns>
    public static (GateVerdict Verdict, string? Reason) Check(
        PluginManifest manifest,
        SemanticVersion? hostVersion)
    {
        if (!manifest.IsEnabled)
            return (GateVerdict.SkipDisabled, $"Plugin '{manifest.Id}' is disabled (IsEnabled=false).");

        if (manifest.MinHostVersion is { } min && hostVersion is { } host && host < min)
            return (GateVerdict.Reject,
                $"Host version {host} is lower than plugin '{manifest.Id}' required minimum {min}.");

        return (GateVerdict.Accept, null);
    }
}
