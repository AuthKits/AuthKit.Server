using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthKit.Plugins.Abstractions.Contracts.Discovery;
using AuthKit.Plugins.Abstractions.Models;

namespace Host.Plugins.Loading;

/// <summary>
/// File-backed discovery cache. Stores manifests with per-location source
/// fingerprints; any input change invalidates the whole file.
/// </summary>
/// <remarks>
/// <para>
/// Cached data is discovery metadata only (manifest + location + fingerprint),
/// never load contexts, types, or instances.
/// </para>
/// <para>
/// Best-effort persistence: corrupt or unreadable cache files are treated as a
/// miss, and store failures are logged without failing startup.
/// </para>
/// </remarks>
/// <param name="cacheFilePath">Where the cache file lives.</param>
/// <param name="logger">The logger used to report cache hits, misses, and failures.</param>
public sealed class FilePluginDiscoveryCache(string cacheFilePath, ILogger logger) : IPluginDiscoveryCache
{
    private const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    /// <summary>
    /// Returns cached discovery results or null on cache miss or staleness.
    /// </summary>
    /// <param name="cancellationToken">Stops fingerprint verification between entries.</param>
    /// <returns>Cached candidates or null to trigger full discovery.</returns>
    public Task<IReadOnlyList<DiscoveredPlugin>?> TryGetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(cacheFilePath))
                return Miss();

            using var stream = File.OpenRead(cacheFilePath);
            var stored = JsonSerializer.Deserialize<CacheFile>(stream, Options);
            if (stored is null || stored.FormatVersion != SchemaVersion)
                return Miss();

            var entries = new List<DiscoveredPlugin>();
            foreach (var entry in stored.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Fingerprint(entry.Location) != entry.SourceFingerprint)
                {
                    logger.LogInformation("Discovery cache stale for '{Location}'; full rediscovery.", entry.Location);
                    return Miss();
                }

                entries.Add(new DiscoveredPlugin
                {
                    Manifest = entry.Manifest is { } manifest ? Normalize(manifest) : null,
                    Location = entry.Location,
                    DiscoveryError = entry.DiscoveryError,
                });
            }

            logger.LogInformation("Discovery cache hit: {Count} plugins reused.", entries.Count);
            return Task.FromResult<IReadOnlyList<DiscoveredPlugin>?>(entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Discovery cache unreadable; falling back to full discovery.");
            return Miss();
        }
    }

    /// <summary>
    /// Stores fresh discovery results for future hits.
    /// </summary>
    /// <param name="plugins">Freshly discovered candidates.</param>
    /// <param name="cancellationToken">Token (best-effort store ignores cancellation mid-write).</param>
    public Task StoreAsync(IReadOnlyList<DiscoveredPlugin> plugins, CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (directory is not null)
                Directory.CreateDirectory(directory);

            var file = new CacheFile(
                SchemaVersion,
                plugins.Select(candidate => new CacheEntry(
                    candidate.Location,
                    Fingerprint(candidate.Location),
                    candidate.Manifest,
                    candidate.DiscoveryError)).ToList());

            using var stream = File.Create(cacheFilePath);
            JsonSerializer.Serialize(stream, file, Options);
            logger.LogInformation("Discovery cache stored: {Count} plugins.", plugins.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Discovery cache store failed; continuing without cache.");
        }

        return Task.CompletedTask;
    }

    private static string Fingerprint(string location)
    {
        using var sha = SHA256.Create();
        var files = Directory.Exists(location)
            ? Directory.GetFiles(location, "*.json")
                .Concat(Directory.GetFiles(location, "*.dll"))
                .Order(StringComparer.Ordinal)
                .ToList()
            : new List<string>();

        foreach (var file in files)
        {
            var name = Encoding.UTF8.GetBytes(Path.GetFileName(file));
            sha.TransformBlock(name, 0, name.Length, null, 0);
            try
            {
                using var stream = File.OpenRead(file);
                var buffer = new byte[8192];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    sha.TransformBlock(buffer, 0, read, null, 0);
            }
            catch (IOException)
            {
                sha.TransformBlock([0], 0, 1, null, 0);
            }
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    private static PluginManifest Normalize(PluginManifest manifest) =>
        manifest with
        {
            Capabilities = new HashSet<string>(manifest.Capabilities, StringComparer.OrdinalIgnoreCase),
            Tags = manifest.Tags.ToArray(),
            DependsOn = manifest.DependsOn.ToArray(),
        };

    private static Task<IReadOnlyList<DiscoveredPlugin>?> Miss() =>
        Task.FromResult<IReadOnlyList<DiscoveredPlugin>?>(null);

    private sealed record CacheFile(int FormatVersion, List<CacheEntry> Entries);

    private sealed record CacheEntry(
        string Location,
        string SourceFingerprint,
        PluginManifest? Manifest,
        string? DiscoveryError);
}
