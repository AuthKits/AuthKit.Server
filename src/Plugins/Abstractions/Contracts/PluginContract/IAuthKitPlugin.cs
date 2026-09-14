using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

/// <summary>
/// Defines the contract implemented by an AuthKit plugin.
/// </summary>
/// <remarks>
/// <para>
/// AuthKit plugins are discovered and loaded dynamically by the host at startup.
/// A plugin does not need to be directly referenced by the host project.
/// </para>
/// <para>
/// The plugin contract allows an extension to contribute services, middleware,
/// health checks, and OpenAPI security scheme metadata to the host application.
/// </para>
/// <para>
/// Plugin implementations should keep their integration with the host limited
/// to the abstractions exposed by this contract and should register any
/// plugin-specific dependencies through <see cref="ConfigureServices"/>.
/// </para>
/// <para>
/// The contract surface is split into cohesive partial declarations grouped
/// into the <c>IAuthKitPlugin*.cs</c> files in this folder: the base identity
/// and metadata accessors, host requirements, configuration, health, pipeline,
/// security, and lifecycle hooks.
/// </para>
/// </remarks>
public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Gets the stable, host-unique identifier of the plugin.
    /// </summary>
    /// <remarks>
    /// The ID is an author-declared identifier (e.g. "authkit.devtokens"),
    /// is expected to be non-empty and stable across restarts. The host is
    /// responsible for validating format and uniqueness before activation.
    /// </remarks>
    string Id => Metadata.Id;

    /// <summary>
    /// Gets the unique name of the plugin.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the plugin in host diagnostics,
    /// startup output, and other plugin-related metadata.
    /// </remarks>
    string Name => Metadata.Name;

    /// <summary>
    /// Gets an optional human-readable display name for UIs.
    /// </summary>
    /// <remarks>
    /// The host UI should display <c>DisplayName ?? Name</c> when presenting
    /// the plugin to users.
    /// </remarks>
    string? DisplayName => Metadata.DisplayName;

    /// <summary>
    /// Gets an optional human-readable description of the plugin.
    /// </summary>
    /// <remarks>
    /// The host may display the description in startup output,
    /// diagnostics, administrative interfaces, or other status surfaces.
    /// </remarks>
    string Description => Metadata.Description;

    /// <summary>
    /// Gets the version of the plugin as a semantic version (SemVer 2.0.0).
    /// </summary>
    /// <remarks>
    /// The Version replaces the previous string-based version and exposes
    /// full semantic version semantics (parsing, equality, precedence).
    /// </remarks>
    SemanticVersion Version => SemanticVersion.Parse(Metadata.Version);

    /// <summary>
    /// Optional author metadata, visible in catalogs and diagnostics.
    /// </summary>
    string? Author => Metadata.Author;

    /// <summary>
    /// Optional SPDX-style license string (no validation performed by host).
    /// </summary>
    string? License => Metadata.License;

    /// <summary>
    /// Optional absolute URI pointing to the license text.
    /// </summary>
    string? LicenseUrl => Metadata.LicenseUrl;

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to a plugin homepage.
    /// </summary>
    string? Homepage => Metadata.Homepage;

    /// <summary>
    /// Optional absolute HTTP/HTTPS URI pointing to the plugin repository.
    /// </summary>
    string? RepositoryUrl => Metadata.RepositoryUrl;

    /// <summary>
    /// Optional classification tags for UI filtering. Defaults to empty.
    /// Null or whitespace elements are invalid and should be rejected during validation.
    /// Used for filtering plugins in UIs and catalogs.
    /// </summary>
    /// <remarks>
    /// Tags are case-sensitive strings without controlled vocabulary.
    /// Example: ["security", "auth", "audit"].
    /// </remarks>
    IReadOnlyList<string> Tags => Metadata.Tags;

    /// <summary>
    /// Priority used for activation ordering among dependency-ready plugins.
    /// Lower values are activated earlier, higher values later.
    /// Defaults to 0.
    /// </summary>
    /// <remarks>
    /// The ordering algorithm is: topological sort where, among the set of currently
    /// dependency-ready plugins, the next plugin is chosen by Priority ascending.
    /// Dependency order (G7) wins over Priority.
    /// Example: A (p. 100) → B (p-100, DependsOn A), C (p. 0) ⇒ order: A, C, B.
    /// </remarks>
    int Priority => Metadata.Priority;

    /// <summary>
    /// Indicates whether the plugin is enabled. Defaults to true.
    /// </summary>
    /// <remarks>
    /// If <c>false</c>, the plugin is skipped before loading (no consistency check runs for it).
    /// For plugins accepted by the preload gate and subsequently loaded,
    /// <c>manifest.IsEnabled == instance.IsEnabled</c> is part of consistency validation.
    /// </remarks>
    bool IsEnabled => Metadata.IsEnabled;

    /// <summary>
    /// Features/capabilities exposed by the plugin. Contract
    /// requires Case-insensitive comparison. Defaults to an immutable empty set.
    /// </summary>
    /// <remarks>
    /// Used for pre-activation capability checks (via <see cref="PluginManifest"/>) and
    /// post-load consistency validation. Host checks capabilities using the
    /// <see cref="PluginExtensions.Supports(PluginContract.IAuthKitPlugin,string)"/> extension method.
    /// Example: <c>plugin.Supports("auth")</c>.
    /// </remarks>
    IReadOnlySet<string> Capabilities =>
        System.Collections.Immutable.ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, Metadata.Capabilities);

    /// <summary>
    /// Gets the metadata associated with the plugin.
    /// </summary>
    PluginMetadataAttribute Metadata => GetType().GetCustomAttribute<PluginMetadataAttribute>()
        ?? throw new InvalidOperationException($"Plugin {GetType().Name} is missing [PluginMetadata] attribute.");
}