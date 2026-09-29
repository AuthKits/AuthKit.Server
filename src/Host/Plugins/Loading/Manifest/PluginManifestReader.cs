using System.Text.Json;
using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Loading.Manifest;

/// <summary>
/// Reads plugin manifest from plugin directory without loading any assembly.
/// </summary>
/// <remarks>
/// <para>
/// Looks for <c>plugin.json</c>, <c>plugin.manifest</c>, then <c>manifest.json</c>
/// (first match wins). A manifest is required: directories without one are
/// invalid candidates.
/// </para>
/// <para>
/// Deserialization is key case insensitive, and collections are snapshotted
/// into arrays with case insensitive capability sets, so later stages observe
/// stable, normalized metadata.
/// </para>
/// </remarks>
internal static class PluginManifestReader
{
    private static readonly string[] FileNames = ["plugin.json", "plugin.manifest", "manifest.json"];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Tries to read the manifest from <paramref name="pluginDirectory"/>.
    /// </summary>
    /// <param name="pluginDirectory">One plugin directory.</param>
    /// <param name="manifest">The normalized manifest or null on failure.</param>
    /// <param name="error">The failure reason or null on success.</param>
    /// <returns>
    /// True with valid manifest. False with <paramref name="error"/> set when
    /// no manifest file exists or it cannot be parsed.
    /// </returns>
    public static bool TryRead(
        string pluginDirectory,
        out PluginManifest? manifest,
        out string? error)
    {
        var path = FileNames
            .Select(name => Path.Combine(pluginDirectory, name))
            .FirstOrDefault(File.Exists);

        if (path is null)
        {
            manifest = null;
            error = $"No manifest file found in '{pluginDirectory}' (expected plugin.json, plugin.manifest, or manifest.json).";
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            var read = JsonSerializer.Deserialize<PluginManifest>(json, Options)
                ?? throw new JsonException("Manifest deserialized to null.");

            manifest = Normalize(read);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            manifest = null;
            error = $"Manifest '{Path.GetFileName(path)}' is unreadable: {ex.Message}";
            return false;
        }
    }

    private static PluginManifest Normalize(PluginManifest manifest) =>
        manifest with
        {
            Capabilities = new HashSet<string>(manifest.Capabilities, StringComparer.OrdinalIgnoreCase),
            Tags = [.. manifest.Tags],
            DependsOn = [.. manifest.DependsOn],
        };
}
