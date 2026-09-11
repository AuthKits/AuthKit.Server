namespace AuthKit.Plugins.Abstractions.Contracts.Plugins;

/// <summary>
/// Declares the identity, versioning, and cataloging metadata for an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is consumed by the <c>IAuthKitPlugin</c> contract to supply the
/// plugin's <c>Name</c>, <c>Version</c>, and <c>Description</c> surface, so plugin
/// classes only declare their metadata in one place.
/// </para>
/// <para>
/// <see cref="Id"/> is the stable machine identity of the plugin, <see cref="Name"/>
/// is the plugin name, <see cref="DisplayName"/> is an optional UI label, and
/// <see cref="Description"/> describes the functionality provided by the plugin.
/// </para>
/// <para>
/// <see cref="Tags"/> categorize the plugin, <see cref="DependsOn"/> list the ids of
/// plugins this plugin requires to operate, and <see cref="Capabilities"/> describe
/// the functional capabilities contributed to the host.
/// </para>
/// <para>
/// Publishing metadata (<see cref="Author"/>, <see cref="License"/>,
/// <see cref="LicenseUrl"/>, <see cref="Homepage"/>, <see cref="RepositoryUrl"/>)
/// documents the plugin's origin and distribution surface.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PluginMetadataAttribute : Attribute
{
    /// <summary>
    /// Creates plugin metadata.
    /// </summary>
    /// <param name="id">The stable machine identity of the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <param name="name">The name of the plugin.</param>
    /// <param name="tags">Categories used to describe the plugin.</param>
    /// <param name="dependsOn">Ids of plugins this plugin depends on.</param>
    /// <param name="capabilities">Functional capabilities contributed to the host.</param>
    /// <param name="displayName">Optional human-readable UI label.</param>
    /// <param name="description">Optional description of the plugin's functionality.</param>
    /// <param name="author">Optional plugin author or maintainer.</param>
    /// <param name="license">Optional license identifier.</param>
    /// <param name="licenseUrl">Optional URL pointing to the license text.</param>
    /// <param name="homepage">Optional URL of the plugin's homepage.</param>
    /// <param name="repositoryUrl">Optional URL of the plugin's source repository.</param>
    public PluginMetadataAttribute(
        string id,
        string version,
        string name,
        string[]? tags = null,
        string[]? dependsOn = null,
        string[]? capabilities = null,
        string? displayName = null,
        string? description = null,
        string? author = null,
        string? license = null,
        string? licenseUrl = null,
        string? homepage = null,
        string? repositoryUrl = null)
    {
        Id = id;
        Version = version;
        Name = name;
        Tags = tags ?? [];
        DependsOn = dependsOn ?? [];
        Capabilities = capabilities ?? [];
        DisplayName = displayName;
        Description = description;
        Author = author;
        License = license;
        LicenseUrl = licenseUrl;
        Homepage = homepage;
        RepositoryUrl = repositoryUrl;
    }

    /// <summary>Gets the stable machine identity of the plugin.</summary>
    public string Id { get; }

    /// <summary>Gets the version of the plugin.</summary>
    public string Version { get; }

    /// <summary>Gets the name of the plugin.</summary>
    public string Name { get; }

    /// <summary>Gets the functional capabilities contributed to the host.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>Gets the ids of plugins this plugin depends on.</summary>
    public IReadOnlyList<string> DependsOn { get; }

    /// <summary>Gets the functional capabilities contributed to the host.</summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>Gets the optional human-readable UI label.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional description of the plugin's functionality.</summary>
    public string? Description { get; }

    /// <summary>Gets the optional plugin author or maintainer.</summary>
    public string? Author { get; }

    /// <summary>Gets the optional license identifier.</summary>
    public string? License { get; }

    /// <summary>Gets the optional URL pointing to the license text.</summary>
    public string? LicenseUrl { get; }

    /// <summary>Gets the optional URL of the plugin's homepage.</summary>
    public string? Homepage { get; }

    /// <summary>Gets the optional URL of the plugin's source repository.</summary>
    public string? RepositoryUrl { get; }
}