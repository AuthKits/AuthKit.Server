using System.Reflection;
using AuthKit.Plugins.Abstractions.Models;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace Host.Plugins.Loading;

/// <summary>
/// Represents a plugin loaded from disk together with its contract instance,
/// assembly, and source directory.
/// </summary>
/// <remarks>
/// <para>
/// The host uses the loaded assembly to register plugin provided
/// Wolverine handlers and MVC application parts.
/// </para>
/// <para>
/// The plugin directory is retained for diagnostics and identifying the
/// location from which the plugin was loaded.
/// </para>
/// <para>
/// The manifest is the same instance the candidate was discovered with,
/// already verified consistent with <see cref="Plugin"/> by the pipeline.
/// </para>
/// </remarks>
/// <param name="Plugin">The loaded AuthKit plugin contract instance.</param>
/// <param name="Assembly">The assembly containing the loaded plugin.</param>
/// <param name="PluginDirectory">The directory from which the plugin was loaded.</param>
/// <param name="Manifest">The manifest the plugin was discovered with.</param>
public sealed record LoadedPlugin(
    IAuthKitPlugin Plugin,
    Assembly Assembly,
    string PluginDirectory,
    PluginManifest? Manifest = null);