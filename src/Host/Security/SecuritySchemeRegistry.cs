using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Host.Plugins;

namespace Host.Security;

/// <summary>
/// The default <see cref="ISecuritySchemeRegistry"/> built from the security
/// schemes contributed by all loaded plugins.
/// </summary>
/// <remarks>
/// <para>
/// Scheme names are unique across plugins. If two plugins declare the same
/// scheme name the registry treats it as a host configuration error rather
/// than silently resolving to either plugin's descriptor.
/// </para>
/// </remarks>
public sealed class SecuritySchemeRegistry : ISecuritySchemeRegistry
{
    private readonly IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> _schemes;

    /// <summary>
    /// Creates a registry over the supplied loaded plugins.
    /// </summary>
    /// <param name="plugins">The plugins contributing security schemes.</param>
    /// <exception cref="InvalidOperationException">
    /// More than one plugin declares the same security scheme name.
    /// </exception>
    public SecuritySchemeRegistry(IReadOnlyList<LoadedPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        _schemes = BuildSchemeMap(plugins);
    }

    private static IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> BuildSchemeMap(
        IReadOnlyList<LoadedPlugin> plugins)
    {
        var map = new Dictionary<string, AuthKitSecuritySchemeDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            foreach (var (name, descriptor) in plugin.Plugin.GetSecuritySchemes())
            {
                if (!map.TryAdd(name, descriptor))
                {
                    throw new InvalidOperationException(
                        $"Security scheme '{name}' is declared by more than one plugin. " +
                        $"Scheme names must be unique across all enabled plugins.");
                }
            }
        }

        return map;
    }

    /// <inheritdoc />
    public bool TryGet(string schemeName, out AuthKitSecuritySchemeDescriptor result) =>
        _schemes.TryGetValue(schemeName, out result!);
}