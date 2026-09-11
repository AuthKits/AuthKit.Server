namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Atrybut metadanych pluginu AuthKit.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginMetadataAttribute(
    string id,
    string version,
    string[] tags,
    string[]? dependsOn,
    string[] capabilities,
    string? name = null,
    string? displayName = null,
    string? description = null,
    string? author = null,
    string? license = null,
    string? licenseUrl = null,
    string? homepage = null,
    string? repositoryUrl = null,
    int priority = 0,
    bool isEnabled = true,
    string? minHostVersion = null)
    : Attribute
{
    public string Id { get; } = id;
    public string Name { get; } = name ?? id;
    public string? DisplayName { get; } = displayName;
    public string Description { get; } = description ?? throw new ArgumentException("Description cannot be empty.");
    public string Version { get; } = version;
    public string? Author { get; } = author;
    public string? License { get; } = license;
    public string? LicenseUrl { get; } = licenseUrl;
    public string? Homepage { get; } = homepage;
    public string? RepositoryUrl { get; } = repositoryUrl;
    public string[] Tags { get; } = tags ?? throw new ArgumentException("Tags cannot be empty.");
    public string[] DependsOn { get; } = dependsOn ?? [];
    public string[] Capabilities { get; } = capabilities ?? throw new ArgumentException("Capabilities cannot be empty.");
    public int Priority { get; } = priority;
    public bool IsEnabled { get; } = isEnabled;
    public string? MinHostVersion { get; } = minHostVersion;
}