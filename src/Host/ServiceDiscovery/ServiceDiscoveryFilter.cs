namespace Host.ServiceDiscovery;

using System.Reflection;

/// <summary>
/// Filters types during service discovery for automatic DI registration.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Detects type category based on namespace markers (DTO, Entity, Repository, Services, etc.).</item>
/// <item>Extracts feature name from the namespace segment right after the layer root.</item>
/// <item>Extracts layer from the namespace root (Application, Domain, Infrastructure, etc.).</item>
/// <item>Applies <see cref="ServiceDiscoveryOptions"/> including allowed/excluded namespaces, layers, interfaces, exceptions, and types.</item>
/// <item>Logs clear debug message indicating decision, layer, type category, feature, and full type name.</item>
/// </list>
/// </remarks>
public static class ServiceDiscoveryFilter
{
    private static readonly (string Key, string Name)[] NamespaceTypes =
    [
        (".DTO", "DTO"),
        (".Entity", "Entity"),
        (".ValueObject", "ValueObject"),
        (".Services", "Service"),
        (".Managers", "Manager"),
        (".Repositories", "Repository"),
        (".Validators", "Validator"),
        (".Middleware", "Middleware"),
        (".Controller", "Controller")
    ];

    /// <summary>
    /// Determines if type is valid for automatic DI registration.
    /// </summary>
    /// <param name="type">Type to evaluate.</param>
    /// <param name="logger">Logger for debug output.</param>
    /// <param name="opts">Service discovery options controlling inclusion/exclusion rules.</param>
    /// <returns><c>true</c> if the type should be included; otherwise <c>false</c>.</returns>
    public static bool IsValidServiceType(Type type, ILogger logger, ServiceDiscoveryOptions opts)
    {
        var ns = type.Namespace ?? string.Empty;
        var layer = GetLayer(ns);
        var feature = GetFeature(ns);
        var detectedType = GetDetectedType(ns);

        bool include = ShouldInclude(type, ns, layer, opts);

        if (opts.EnableLogging)
        {
            logger.LogDebug(
                "{Decision} Layer({Layer}) Type({TypeKind}) Feature({Feature}): {Type}",
                include ? "Including" : "Skipping",
                layer,
                detectedType,
                feature,
                type.FullName);
        }

        return include;
    }

    /// <summary>
    /// Returns the first namespace segment (layer), e.g. "Application", "Domain".
    /// </summary>
    private static string GetLayer(string ns)
        => ns.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "Unknown";

    /// <summary>
    /// Detects type group (DTO, Entity, Service, etc.) based on namespace markers.
    /// </summary>
    private static string GetDetectedType(string ns)
    {
        foreach (var (key, name) in NamespaceTypes)
        {
            if (ns.Contains(key))
                return name;
        }

        return "Allowed";
    }

    /// <summary>
    /// Extracts feature (the namespace segment right after the layer root, e.g.
    /// "KeyManagement" from "Core.KeyManagement.Services") or returns "Global".
    /// </summary>
    private static string GetFeature(string ns)
    {
        var parts = ns.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : "Global";
    }

    private static bool IsRecord(Type type)
        => type.GetMethod(
               "<Clone>$",
               BindingFlags.Public | BindingFlags.Instance) is not null;

    /// <summary>
    /// Applies <see cref="ServiceDiscoveryOptions"/> rules to decide inclusion.
    /// </summary>
    private static bool ShouldInclude(Type type, string ns, string layer, ServiceDiscoveryOptions opts)
        => !IsRecord(type)
           && !(opts.SkipInterfaces && type.IsInterface)
           && !(opts.SkipExceptions && typeof(Exception).IsAssignableFrom(type))
           && !IsRecordLike(type)
           && !opts.ExcludedTypes.Contains(type)
           && !opts.ExcludedNamespaces.Any(ns.Contains)
           && (opts.AllowedNamespaces.Count == 0 || opts.AllowedNamespaces.Any(ns.Contains))
           && (opts.AllowedLayers.Count == 0 || opts.AllowedLayers.Contains(layer));

    /// <summary>
    /// Detects record types (value objects, DTOs, entities) which are data
    /// structures rather than injectable services.
    /// </summary>
    /// <remarks>
    /// Records implicitly implement <see cref="IEquatable{T}"/> for their own type.
    /// Such types are constructed explicitly by application code and must not be
    /// registered as services, since their constructors are not resolvable from
    /// the dependency injection container.
    /// </remarks>
    private static bool IsRecordLike(Type type)
        => type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEquatable<>) &&
            i.GetGenericArguments()[0] == type);
}
