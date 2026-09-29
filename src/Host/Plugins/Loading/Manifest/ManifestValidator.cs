using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Loading.Manifest;

/// <summary>
/// Structural pre load validation of discovered manifests. Runs before any
/// plugin assembly is loaded every failure rejects the candidate.
/// </summary>
/// <remarks>
/// <para>
/// Only shape is checked here: presence, emptiness, duplicates, and
/// self dependencies. Semantic checks needing the loaded instance (consistency)
/// or the host (compatibility gate) belong to later pipeline stages.
/// </para>
/// </remarks>
internal static class ManifestValidator
{
    /// <summary>
    /// Validates one manifest structurally. Returns zero or more error messages.
    /// </summary>
    /// <param name="manifest">The discovered manifest.</param>
    /// <returns>Structural violations; empty when the manifest is well-formed.</returns>
    public static IReadOnlyList<string> Validate(PluginManifest manifest)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.Id))
            errors.Add("Id must not be empty.");

        if (string.IsNullOrWhiteSpace(manifest.Name))
            errors.Add("Name must not be empty.");

        if (manifest.Tags.Any(string.IsNullOrWhiteSpace))
            errors.Add("Tags must not contain null or whitespace elements.");

        if (manifest.DependsOn.Any(string.IsNullOrWhiteSpace))
            errors.Add("DependsOn must not contain null or whitespace entries.");

        if (manifest.DependsOn.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.DependsOn.Count)
            errors.Add("DependsOn must not contain duplicates.");

        if (manifest.DependsOn.Contains(manifest.Id, StringComparer.OrdinalIgnoreCase))
            errors.Add($"Plugin must not depend on itself ('{manifest.Id}').");

        return errors;
    }

    /// <summary>
    /// Finds duplicate manifest Ids (case-insensitive) across the discovered set.
    /// </summary>
    /// <param name="manifests">Structurally valid manifests.</param>
    /// <returns>Ids claimed by more than one manifest.</returns>
    public static IReadOnlySet<string> FindDuplicateIds(IEnumerable<PluginManifest> manifests) =>
        manifests
            .GroupBy(manifest => manifest.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
