using System;

namespace AuthKit.ManifestGenerator.Core.Providers;

/// <summary>
/// Defines the contract for resolving plugin type from plugin assembly.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for loading the specified assembly and
/// identifying the type that represents the plugin implementation.
/// </para>
/// <para>
/// The resolver separates plugin type discovery from manifest generation,
/// allowing the resolved plugin type to be consumed by metadata providers
/// and other components without coupling them to assembly loading logic.
/// </para>
/// </remarks>
public interface IPluginTypeResolver
{
    /// <summary>
    /// Resolves the plugin type from the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the plugin assembly from which the plugin type is resolved.</param>
    /// <returns>
    /// The <see cref="Type"/> representing the plugin implementation found
    /// in the specified assembly.
    /// </returns>
    Type GetPluginType(string assemblyPath);
}