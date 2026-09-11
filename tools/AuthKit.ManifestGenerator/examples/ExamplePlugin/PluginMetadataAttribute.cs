using System;

namespace ExamplePlugin;

/// <summary>
/// Specifies metadata used to identify and describe an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// The attribute is applied to a plugin implementation and provides the
/// metadata required by the manifest generation pipeline.
/// </para>
/// <para>
/// The plugin identifier, version, tags, capabilities, and dependencies are
/// supplied explicitly. When an optional display name or plugin name is not
/// provided, the plugin identifier is used as the default name.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class PluginMetadataAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier of the plugin.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the name of the plugin.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional display name of the plugin.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the description of the plugin.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the semantic version of the plugin.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the tags associated with the plugin.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Gets the identifiers of plugins that this plugin depends on.
    /// </summary>
    public string[] DependsOn { get; }

    /// <summary>
    /// Gets the capabilities provided by the plugin.
    /// </summary>
    public string[] Capabilities { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMetadataAttribute"/> class.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the plugin.
    /// </param>
    /// <param name="version">The semantic version of the plugin. </param>
    /// <param name="tags">The tags associated with the plugin. </param>
    /// <param name="capabilities">The capabilities provided by the plugin. </param>
    /// <param name="dependsOn">The identifiers of plugins required by this plugin. </param>
    /// <param name="name">The optional name of the plugin. When omitted, <paramref name="id"/>
    /// is used as the plugin name.
    /// </param>
    /// <param name="displayName">The optional user facing display name of the plugin. </param>
    /// <param name="description">The description of the plugin. </param>
    /// <exception cref="ArgumentException">Thrown when the description, tags, or capabilities are not provided. </exception>
    public PluginMetadataAttribute(
        string id,
        string version,
        string[] tags,
        string[] capabilities,
        string[] dependsOn,
        string? name = null,
        string? displayName = null,
        string? description = null)
    {
        Id = id;
        Name = name ?? id;
        DisplayName = displayName;
        Description = description
                      ?? throw new ArgumentException("Description cannot be empty.");
        Version = version;
        Tags = tags
               ?? throw new ArgumentException("Tags cannot be empty.");
        DependsOn = dependsOn ?? Array.Empty<string>();
        Capabilities = capabilities
                       ?? throw new ArgumentException("Capabilities cannot be empty.");
    }
}
