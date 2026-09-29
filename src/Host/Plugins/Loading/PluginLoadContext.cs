using System.Reflection;
using System.Runtime.Loader;

namespace Host.Plugins.Loading;

/// <summary>
/// Isolated load context for one plugin. Host and framework assemblies are
/// shared with the default context; only plugin-private dependencies resolve
/// from the plugin directory.
/// </summary>
/// <remarks>
/// <para>
/// Resolution order per assembly name: well-known shared contracts, then
/// anything already loaded in the default context, then anything shipped with
/// the host application directory (shared, returned as null to fall back),
/// otherwise the plugin directory via
/// <see cref="AssemblyDependencyResolver"/>, otherwise default fallback.
/// </para>
/// <para>
/// The shared-contract list matters most at startup: plugin loading runs
/// before first gRPC use, so without it a plugin would privately load its own
/// <c>Grpc.Core.Api</c> copy and its interceptors would fail the host's
/// <c>Interceptor</c> identity check.
/// </para>
/// <para>
/// Collectible to enable future unload/hot-reload orchestration; the loader
/// itself never unloads.
/// </para>
/// </remarks>
/// <param name="entryAssemblyPath">The plugin entry assembly path.</param>
internal sealed class PluginLoadContext(string entryAssemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    private static readonly HashSet<string> SharedContracts = new(StringComparer.OrdinalIgnoreCase)
    {
        "AuthKit.Plugins.Abstractions",
        "Grpc.Core.Api",
        "Google.Protobuf",
    };

    private readonly AssemblyDependencyResolver _resolver = new(entryAssemblyPath);

    /// <summary>
    /// Loads the entry assembly into this context.
    /// </summary>
    public Assembly LoadEntry() => LoadFromAssemblyPath(entryAssemblyPath);

    /// <summary>
    /// Resolves the assembly: shared when listed, already loaded by default,
    /// or shipped with the host; otherwise from the plugin directory,
    /// otherwise default fallback.
    /// </summary>
    /// <param name="assemblyName">The requested assembly name.</param>
    /// <returns>The resolved assembly or null to fall back to the default resolution.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null
            && (SharedContracts.Contains(assemblyName.Name)
                || IsLoadedByDefault(assemblyName.Name)
                || IsShippedWithHost(assemblyName.Name)))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    private static bool IsLoadedByDefault(string name) =>
        Default.Assemblies.Any(candidate =>
            string.Equals(candidate.GetName().Name, name, StringComparison.Ordinal));

    /// <summary>
    /// Host-owned assemblies unify even when the host has not touched them yet
    /// (lazy loading): a plugin must see the same <c>Core</c> types the host
    /// registers in DI, never a plugin-local copy.
    /// </summary>
    private static bool IsShippedWithHost(string name) =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, $"{name}.dll"));
}
