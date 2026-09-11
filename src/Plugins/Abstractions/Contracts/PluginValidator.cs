using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts;

/// <summary>
/// Provides validation logic for plugin consistency between manifest and instance.
/// </summary>
public static class PluginValidator
{
    /// <summary>
    /// Validates that a tag is not null or whitespace.
    /// </summary>
    /// <param name="tag">The tag to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the tag is null or whitespace.</exception>
    private static void ValidateTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tags cannot be null or whitespace.", nameof(tag));
        }
    }
        
    /// <summary>
    /// Validates that all tags in a list are valid (non-null and non-whitespace).
    /// </summary>
    /// <param name="tags">The list of tags to validate.</param>
    /// <exception cref="ArgumentException">Thrown if any tag is null or whitespace.</exception>
    public static void ValidateTags(IReadOnlyList<string> tags)
    {
        if (tags == null)
        {
            throw new ArgumentNullException(nameof(tags), "Tags list cannot be null.");
        }
            
        foreach (var tag in tags)
        {
            ValidateTag(tag);
        }
    }
        
    /// <summary>
    /// Validates that a plugin ID is not empty and follows the expected format.
    /// </summary>
    /// <param name="pluginId">The plugin ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown if the plugin ID is null, empty, or whitespace.</exception>
    private static void ValidatePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentException("Plugin ID cannot be null, empty, or whitespace.", nameof(pluginId));
        }
    }
        
    /// <summary>
    /// Validates that a list of dependency IDs is valid.
    /// </summary>
    /// <param name="dependsOn">The list of dependency IDs to validate.</param>
    /// <param name="pluginId">The ID of the plugin being validated (to check for self-dependency).</param>
    /// <exception cref="ArgumentException">Thrown if any dependency ID is invalid or if there are duplicates.</exception>
    /// <exception cref="InvalidOperationException">Thrown if there is a self-dependency.</exception>
    public static void ValidateDependsOn(IReadOnlyList<string> dependsOn, string pluginId)
    {
        if (dependsOn == null)
        {
            throw new ArgumentNullException(nameof(dependsOn), "Dependencies list cannot be null.");
        }
            
        // Check for duplicates
        var uniqueDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dependencyId in dependsOn)
        {
            ValidatePluginId(dependencyId);
                
            if (!uniqueDependencies.Add(dependencyId))
            {
                throw new ArgumentException("Duplicate dependency ID found: "+ dependencyId);
            }
        }
            
        // Check for self-dependency
        if (dependsOn.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Plugin cannot depend on itself.");
        }
    }

    /// <summary>
    /// Validates that the plugin manifest and instance are consistent.
    /// </summary>
    /// <param name="manifest">The plugin manifest.</param>
    /// <param name="plugin">The plugin instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if either manifest or plugin is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the manifest and instance are inconsistent.</exception>
    public static void ValidateConsistency(PluginManifest manifest, IAuthKitPlugin plugin)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest), "Plugin manifest cannot be null.");
        }
            
        if (plugin == null)
        {
            throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
        }
            
        // Check if IsEnabled matches
        if (manifest.IsEnabled != plugin.IsEnabled)
        {
            throw new InvalidOperationException(
                "Manifest and plugin instance disagree on IsEnabled.");
        }
            
        // Check if Capabilities match (case-insensitive comparison)
        if (!manifest.Capabilities.SetEquals(plugin.Capabilities))
        {
            throw new InvalidOperationException(
                "Manifest and plugin instance have inconsistent capabilities.");
        }
            
        // Check if MinHostVersion matches
        if (manifest.MinHostVersion != plugin.MinHostVersion)
        {
            throw new InvalidOperationException(
                "Manifest and plugin instance disagree on MinHostVersion.");
        }
            
        // Check if DependsOn matches (case-insensitive comparison)
        if (manifest.DependsOn.Count != plugin.DependsOn.Count)
        {
            throw new InvalidOperationException(
                "Manifest and plugin instance have different number of dependencies.");
        }
            
        // Compare each dependency case-insensitively
        var manifestDependsOnSet = new HashSet<string>(manifest.DependsOn, StringComparer.OrdinalIgnoreCase);
        var pluginDependsOnSet = new HashSet<string>(plugin.DependsOn, StringComparer.OrdinalIgnoreCase);
            
        if (!manifestDependsOnSet.SetEquals(pluginDependsOnSet))
        {
            throw new InvalidOperationException(
                "Manifest and plugin instance have inconsistent dependencies.");
        }
    }
}