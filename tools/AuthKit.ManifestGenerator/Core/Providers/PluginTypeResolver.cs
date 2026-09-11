using System;
using System.Linq;
using System.Reflection;
using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.ManifestGenerator.Core.Providers;

/// <summary>
/// Resolves concrete plugin types from plugin assemblies.
/// </summary>
/// <remarks>
/// <para>
/// The resolver loads the specified assembly and searches for concrete,
/// non abstract class implementing <see cref="IAuthKitPlugin"/>.
/// </para>
/// <para>
/// The resolved type is used by the manifest generation pipeline to inspect
/// plugin metadata without requiring the plugin to be instantiated.
/// </para>
/// </remarks>
public sealed class PluginTypeResolver : IPluginTypeResolver
{
    /// <summary>
    /// Resolves the plugin type from the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly containing the plugin implementation. </param>
    /// <returns>
    /// The concrete <see cref="Type"/> implementing
    /// <see cref="IAuthKitPlugin"/> found in the specified assembly.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="assemblyPath"/> is
    /// <see langword="null"/>, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the assembly cannot be loaded or does not contain
    /// concrete implementation of <see cref="IAuthKitPlugin"/>.
    /// </exception>
    public Type GetPluginType(string assemblyPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);

            return assembly.GetTypes()
                       .FirstOrDefault(type =>
                           type.IsClass &&
                           !type.IsAbstract &&
                           typeof(IAuthKitPlugin).IsAssignableFrom(type))
                   ?? throw new InvalidOperationException(
                       $"No concrete {nameof(IAuthKitPlugin)} implementation was found " +
                       $"in assembly '{assembly.FullName}'.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Failed to load plugin assembly '{assemblyPath}'.",
                ex);
        }
    }
}