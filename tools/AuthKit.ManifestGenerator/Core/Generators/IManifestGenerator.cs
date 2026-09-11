using AuthKit.Plugins.Abstractions.Models;

namespace AuthKit.ManifestGenerator.Core.Generators;

/// <summary>
/// Defines the contract for generating plugin manifest files from plugin
/// assemblies or existing <see cref="PluginManifest"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are responsible for producing serialized plugin manifest
/// and writing it to the specified output path.
/// </para>
/// <para>
/// A manifest can be generated either by inspecting a plugin assembly and
/// discovering its metadata, or by serializing an already constructed
/// <see cref="PluginManifest"/> instance.
/// </para>
/// </remarks>
public interface IManifestGenerator
{
    /// <summary>
    /// Generates plugin manifest by inspecting the specified plugin assembly.
    /// </summary>
    /// <param name="pluginAssemblyPath">The path to the plugin assembly from which manifest metadata is discovered. </param>
    /// <param name="outputManifestPath">The path where the generated manifest file is written.</param>
    void Generate(string pluginAssemblyPath, string outputManifestPath);

    /// <summary>
    /// Generates plugin manifest from the specified manifest model.
    /// </summary>
    /// <param name="pluginManifest">The plugin manifest containing the metadata to be serialized.</param>
    /// <param name="outputManifestPath">The path where the generated manifest file is written.</param>
    void Generate(PluginManifest pluginManifest, string outputManifestPath);
}