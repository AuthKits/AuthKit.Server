using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;

namespace AuthKit.PluginContractValidator;

/// <summary>
/// Loads a plugin entry assembly using the same isolation model as the AuthKit host.
/// </summary>
/// <remarks>
/// <para>
/// Each plugin resolves its own dependencies through an <see cref="AssemblyDependencyResolver"/>
/// attached to <see cref="AssemblyLoadContext.Default"/>. The resolver stays attached for the
/// lifetime of the process so that lazily loaded dependencies (for example, types referenced
/// inside <c>CheckHealthAsync</c>) continue to resolve, exactly as in the host.
/// </para>
/// <para>
/// A plugin directory is expected to contain an entry assembly whose file name matches the
/// directory name. Directories without a matching assembly, or assemblies without a public,
/// non-abstract <see cref="IAuthKitPlugin"/> implementation with a parameterless constructor,
/// are reported as load errors rather than thrown.
/// </para>
/// </remarks>
public sealed class PluginAssemblyLoader : IPluginLoader
{
    private static readonly List<AssemblyDependencyResolver> Resolvers = new();
    private static readonly Lock Gate = new();
    private static Func<AssemblyLoadContext, AssemblyName, Assembly?>? _handler;

    /// <summary>
    /// Loads the plugin entry assembly and activates its <see cref="IAuthKitPlugin"/> implementation.
    /// </summary>
    /// <param name="entryDll">The full path to the plugin's entry assembly.</param>
    /// <returns>
    /// A <see cref="PluginLoadResult"/> containing the loaded plugin, or the errors that
    /// prevented loading.
    /// </returns>
    public PluginLoadResult Load(string entryDll)
    {
        var errors = new List<string>();

        try
        {
            var resolver = new AssemblyDependencyResolver(entryDll);
            AttachResolver(resolver);

            Assembly assembly;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(entryDll);
            }
            catch (Exception ex)
            {
                errors.Add($"dependency loading failed: {ex.Message}");
                return new PluginLoadResult(null, errors);
            }

            Type? pluginType;
            try
            {
                pluginType = assembly.GetTypes()
                    .FirstOrDefault(t => t is { IsPublic: true, IsAbstract: false }
                                        && typeof(IAuthKitPlugin).IsAssignableFrom(t)
                                        && t.GetConstructor(Type.EmptyTypes) is not null);
            }
            catch (Exception ex)
            {
                errors.Add($"type discovery failed: {ex.Message}");
                return new PluginLoadResult(null, errors);
            }

            if (pluginType is null)
            {
                errors.Add("no public, non-abstract IAuthKitPlugin implementation with a parameterless constructor");
                return new PluginLoadResult(null, errors);
            }

            IAuthKitPlugin instance;
            try
            {
                instance = (IAuthKitPlugin)Activator.CreateInstance(pluginType)!;
            }
            catch (Exception ex)
            {
                errors.Add($"activation failed: {ex.Message}");
                return new PluginLoadResult(null, errors);
            }

            return new PluginLoadResult(new LoadedPlugin(instance, assembly), errors);
        }
        catch (Exception ex)
        {
            errors.Add($"unexpected error: {ex.Message}");
            return new PluginLoadResult(null, errors);
        }
    }

    /// <summary>
    /// Registers a dependency resolver for <see cref="AssemblyLoadContext.Default"/>, ensuring it
    /// is attached exactly once for the lifetime of the process.
    /// </summary>
    /// <param name="resolver">The dependency resolver associated with a loaded plugin assembly.</param>
    private static void AttachResolver(AssemblyDependencyResolver resolver)
    {
        lock (Gate)
        {
            Resolvers.Add(resolver);
            if (_handler is not null) return;
            
            _handler = (context, name) =>
            {
                for (var i = Resolvers.Count - 1; i >= 0; i--)
                {
                    var path = Resolvers[i].ResolveAssemblyToPath(name);
                    if (path is not null)
                    {
                        return context.LoadFromAssemblyPath(path);
                    }
                }

                return null;
            };

            AssemblyLoadContext.Default.Resolving += _handler;
        }
    }
}
