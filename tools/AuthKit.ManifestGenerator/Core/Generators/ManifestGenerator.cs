using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuthKit.Plugins.Abstractions.Models;
using AuthKit.ManifestGenerator.Core.Providers;

namespace AuthKit.ManifestGenerator.Core.Generators;

/// <summary>
/// Generates serialized plugin manifests from plugin assemblies or existing
/// <see cref="PluginManifest"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// The generator resolves the plugin type from the specified assembly and
/// retrieves its manifest metadata through the configured
/// <see cref="IPluginMetadataProvider"/>.
/// </para>
/// <para>
/// Generated manifests are serialized to JSON with indentation enabled and
/// include a custom converter for <see cref="SemanticVersion"/> values.
/// </para>
/// </remarks>
public class ManifestGenerator : IManifestGenerator
{
    private readonly IPluginTypeResolver _pluginTypeResolver;
    private readonly IPluginMetadataProvider _pluginMetadataProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestGenerator"/> class.
    /// </summary>
    /// <param name="pluginTypeResolver">
    /// The resolver responsible for discovering the plugin type from an assembly.
    /// </param>
    /// <param name="pluginMetadataProvider">
    /// The provider responsible for retrieving manifest metadata from the
    /// resolved plugin type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pluginTypeResolver"/> or
    /// <paramref name="pluginMetadataProvider"/> is <see langword="null"/>.
    /// </exception>
    public ManifestGenerator(
        IPluginTypeResolver pluginTypeResolver,
        IPluginMetadataProvider pluginMetadataProvider)
    {
        _pluginTypeResolver = pluginTypeResolver
            ?? throw new ArgumentNullException(nameof(pluginTypeResolver));

        _pluginMetadataProvider = pluginMetadataProvider
            ?? throw new ArgumentNullException(nameof(pluginMetadataProvider));
    }

    /// <summary>
    /// Generates a plugin manifest by inspecting the specified plugin assembly.
    /// </summary>
    /// <param name="pluginAssemblyPath">
    /// The path to the plugin assembly from which the plugin type and metadata
    /// are resolved.
    /// </param>
    /// <param name="outputManifestPath">
    /// The path where the generated JSON manifest is written.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no plugin type can be resolved from the specified assembly
    /// or when the generated manifest cannot be written to the output path.
    /// </exception>
    public void Generate(
        string pluginAssemblyPath,
        string outputManifestPath)
    {
        Console.WriteLine(
            "Generating manifest for plugin at: {0}",
            pluginAssemblyPath);

        Console.WriteLine(
            "Output manifest path: {0}",
            outputManifestPath);

        var pluginType = _pluginTypeResolver.GetPluginType(pluginAssemblyPath);

        if (pluginType == null)
        {
            throw new InvalidOperationException("Plugin type not found.");
        }

        var manifest = _pluginMetadataProvider.GetPluginManifest(pluginType);

        Console.WriteLine("\n--- Plugin Manifest ---");
        Console.WriteLine("Id: {0}", manifest.Id);
        Console.WriteLine("Name: {0}", manifest.Name);
        Console.WriteLine(
            "DisplayName: {0}",
            manifest.DisplayName ?? "(not set)");
        Console.WriteLine("Description: {0}", manifest.Description);
        Console.WriteLine("Version: {0}", manifest.Version);
        Console.WriteLine(
            "Author: {0}",
            manifest.Author ?? "(not set)");
        Console.WriteLine(
            "License: {0}",
            manifest.License ?? "(not set)");
        Console.WriteLine(
            "LicenseUrl: {0}",
            manifest.LicenseUrl ?? "(not set)");
        Console.WriteLine(
            "Homepage: {0}",
            manifest.Homepage ?? "(not set)");
        Console.WriteLine(
            "RepositoryUrl: {0}",
            manifest.RepositoryUrl ?? "(not set)");
        Console.WriteLine(
            "Tags: {0}",
            string.Join(", ", manifest.Tags));
        Console.WriteLine(
            "DependsOn: {0}",
            string.Join(", ", manifest.DependsOn));
        Console.WriteLine(
            "Capabilities: {0}",
            string.Join(", ", manifest.Capabilities));
        Console.WriteLine("Priority: {0}", manifest.Priority);
        Console.WriteLine("IsEnabled: {0}", manifest.IsEnabled);
        Console.WriteLine(
            "MinHostVersion: {0}",
            manifest.MinHostVersion?.ToString() ?? "(not set)");

        SaveManifestToFile(manifest, outputManifestPath);
    }

    /// <summary>
    /// Generates a plugin manifest from the specified manifest model.
    /// </summary>
    /// <param name="pluginManifest">
    /// The plugin manifest containing the metadata to serialize.
    /// </param>
    /// <param name="outputManifestPath">
    /// The path where the generated JSON manifest is written.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the generated manifest cannot be written to the output path.
    /// </exception>
    public void Generate(
        PluginManifest pluginManifest,
        string outputManifestPath)
    {
        Console.WriteLine("Generating manifest for plugin.");

        Console.WriteLine(
            "Output manifest path: {0}",
            outputManifestPath);

        Console.WriteLine("\n--- Plugin Manifest ---");
        Console.WriteLine("Id: {0}", pluginManifest.Id);
        Console.WriteLine("Name: {0}", pluginManifest.Name);
        Console.WriteLine(
            "DisplayName: {0}",
            pluginManifest.DisplayName ?? "(not set)");
        Console.WriteLine(
            "Description: {0}",
            pluginManifest.Description);
        Console.WriteLine("Version: {0}", pluginManifest.Version);
        Console.WriteLine(
            "Author: {0}",
            pluginManifest.Author ?? "(not set)");
        Console.WriteLine(
            "License: {0}",
            pluginManifest.License ?? "(not set)");
        Console.WriteLine(
            "LicenseUrl: {0}",
            pluginManifest.LicenseUrl ?? "(not set)");
        Console.WriteLine(
            "Homepage: {0}",
            pluginManifest.Homepage ?? "(not set)");
        Console.WriteLine(
            "RepositoryUrl: {0}",
            pluginManifest.RepositoryUrl ?? "(not set)");
        Console.WriteLine(
            "Tags: {0}",
            string.Join(", ", pluginManifest.Tags));
        Console.WriteLine(
            "DependsOn: {0}",
            string.Join(", ", pluginManifest.DependsOn));
        Console.WriteLine(
            "Capabilities: {0}",
            string.Join(", ", pluginManifest.Capabilities));
        Console.WriteLine(
            "Priority: {0}",
            pluginManifest.Priority);
        Console.WriteLine(
            "IsEnabled: {0}",
            pluginManifest.IsEnabled);
        Console.WriteLine(
            "MinHostVersion: {0}",
            pluginManifest.MinHostVersion?.ToString() ?? "(not set)");

        SaveManifestToFile(pluginManifest, outputManifestPath);
    }

    /// <summary>
    /// Serializes the specified plugin manifest and writes it to the output file.
    /// </summary>
    /// <param name="manifest">
    /// The plugin manifest to serialize.
    /// </param>
    /// <param name="outputManifestPath">
    /// The path where the serialized manifest is written.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the manifest cannot be serialized or written to the specified
    /// output path.
    /// </exception>
    private static void SaveManifestToFile(
        PluginManifest manifest,
        string outputManifestPath)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            jsonOptions.Converters.Add(
                new SemanticVersionJsonConverter());

            var json = JsonSerializer.Serialize(
                manifest,
                jsonOptions);

            File.WriteAllText(
                outputManifestPath,
                json);

            Console.WriteLine(
                "\n--- Manifest Generated Successfully ---");

            Console.WriteLine(
                "Manifest saved to: {0}",
                outputManifestPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to save manifest to file.",
                ex);
        }
    }
}