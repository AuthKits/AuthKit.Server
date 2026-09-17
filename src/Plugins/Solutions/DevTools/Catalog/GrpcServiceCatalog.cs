using System.Reflection;
using System.Runtime.Loader;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace DevTools.Catalog;

/// <summary>
/// Provides an in-process catalog of the gRPC services exposed by the
/// host application.
/// </summary>
/// <remarks>
/// <para>
/// Generated gRPC code exposes each service as a static <c>Descriptor</c>
/// property of type <see cref="ServiceDescriptor"/> on the generated static
/// service class (e.g. <c>Host.Greeter.Descriptor</c>). Services are
/// discovered by scanning those properties across the assemblies loaded
/// into the default load context.
/// </para>
/// <para>
/// Message fields are expanded up to <see cref="MaxMessageDepth"/> levels
/// so recursive messages remain finite when rendered by the UI.
/// </para>
/// </remarks>
public sealed class GrpcServiceCatalog(ILogger<GrpcServiceCatalog> logger) : IGrpcServiceCatalog
{
    /// <summary>
    /// Maximum number of message nesting levels expanded when describing
    /// request and response schemas.
    /// </summary>
    private const int MaxMessageDepth = 5;

    private readonly Lazy<(IReadOnlyList<GrpcServiceInfo> Services,
        IReadOnlyDictionary<string, GrpcMethodCatalogEntry> Methods)> _catalog = new(() => Scan(logger));

    /// <summary>
    /// Gets the discovered gRPC services ordered by full name.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="GrpcServiceInfo"/> describing the
    /// discovered services and their methods.
    /// </returns>
    public IReadOnlyList<GrpcServiceInfo> GetServices() => _catalog.Value.Services;

    /// <summary>
    /// Attempts to locate a method by its full name.
    /// </summary>
    /// <param name="serviceName">The full name of the service.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>
    /// The <see cref="GrpcMethodCatalogEntry"/> for the matching method,
    /// or <c>null</c> when no such method exists in the catalog.
    /// </returns>
    public GrpcMethodCatalogEntry? TryGetMethod(string serviceName, string methodName)
    {
        _catalog.Value.Methods.TryGetValue($"{serviceName}/{methodName}", out var entry);
        return entry;
    }

    /// <summary>
    /// Scans the assemblies currently loaded into the default load context and
    /// gathers every gRPC service and method into an ordered, immutable snapshot.
    /// </summary>
    private static (IReadOnlyList<GrpcServiceInfo> Services, IReadOnlyDictionary<string, GrpcMethodCatalogEntry> Methods) Scan(ILogger<GrpcServiceCatalog> logger)
    {
        var services = new List<GrpcServiceInfo>();
        var methods = new Dictionary<string, GrpcMethodCatalogEntry>();
        var seenServices = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in AssemblyLoadContext.Default.Assemblies.ToArray())
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                foreach (var property in type.GetProperties(BindingFlags.Static | BindingFlags.Public))
                {
                    if (property.PropertyType != typeof(ServiceDescriptor) || property.GetIndexParameters().Length != 0)
                        continue;

                    if (property.GetValue(null) is not ServiceDescriptor descriptor || !seenServices.Add(descriptor.FullName))
                        continue;

                    try
                    {
                        var comments = CommentsFor(descriptor.File, assembly, logger);
                        services.Add(BuildServiceInfo(descriptor, comments, assembly));
                        foreach (var method in descriptor.Methods)
                            methods[$"{descriptor.FullName}/{method.Name}"] =
                                new GrpcMethodCatalogEntry(BuildMethodInfo(method, comments), method);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to describe gRPC service {Service}.", descriptor.FullName);
                    }
                }
            }
        }

        logger.LogInformation("gRPC catalog discovered {Count} service(s).", services.Count);
        return (services
            .OrderBy(s => s.FullName, StringComparer.Ordinal)
            .ToArray(), methods);
    }

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }
    }

    private static GrpcServiceInfo BuildServiceInfo(ServiceDescriptor descriptor,
        IReadOnlyDictionary<string, string> comments, Assembly assembly) =>
        new()
        {
            Name = descriptor.Name,
            FullName = descriptor.FullName,
            Description = comments.TryGetValue(descriptor.Name, out var summary) ? summary : null,
            Package = descriptor.File.Package,
            FileName = descriptor.File.Name,
            IsPlugin = IsPluginAssembly(assembly),
            Methods = descriptor.Methods
                .Select(method => BuildMethodInfo(method, comments))
                .ToArray()
        };

    private static bool IsPluginAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? string.Empty;
        if (string.Equals(assemblyName, "Host", StringComparison.OrdinalIgnoreCase) ||
            assembly == Assembly.GetEntryAssembly())
        {
            return false;
        }

        if (assembly.GetCustomAttribute<PluginMetadataAttribute>() is not null)
        {
            return true;
        }

        var types = SafeGetTypes(assembly);
        if (types.Any(t => typeof(IAuthKitPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            return true;
        }

        if (assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("Google.", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(assemblyName, "Core", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return false;
    }

    private static GrpcMethodInfo BuildMethodInfo(MethodDescriptor method,
        IReadOnlyDictionary<string, string> comments) =>
        new()
        {
            Name = method.Name,
            FullName = $"{method.Service.FullName}/{method.Name}",
            Description = comments.TryGetValue($"{method.Service.Name}.{method.Name}", out var summary)
                ? summary
                : null,
            IsClientStreaming = method.IsClientStreaming,
            IsServerStreaming = method.IsServerStreaming,
            Request = BuildMessageSchema(method.InputType, depth: 0, comments),
            Response = BuildMessageSchema(method.OutputType, depth: 0, comments)
        };

    private static GrpcMessageSchema BuildMessageSchema(MessageDescriptor message, int depth,
        IReadOnlyDictionary<string, string> comments) =>
        new()
        {
            Name = message.Name,
            FullName = message.FullName,
            Description = comments.TryGetValue(message.Name, out var summary) ? summary : null,
            Fields = depth >= MaxMessageDepth
                ? []
                : message.Fields.InFieldNumberOrder()
                    .Select(field => BuildFieldSchema(field, depth, comments))
                    .ToArray()
        };

    private static GrpcFieldSchema BuildFieldSchema(FieldDescriptor field, int depth,
        IReadOnlyDictionary<string, string> comments)
    {
        var schema = new GrpcFieldSchema
        {
            Name = field.Name,
            Description = comments.TryGetValue($"{field.ContainingType.Name}.{field.Name}", out var summary)
                ? summary
                : null,
            FieldType = field.FieldType.ToString(),
            IsRepeated = field.IsRepeated,
            IsMap = field.IsMap
        };

        if (field.IsMap)
        {
            var keyField = field.MessageType.Fields.InFieldNumberOrder()[0];
            var valueField = field.MessageType.Fields.InFieldNumberOrder()[1];

            schema.MapKeyType = keyField.FieldType.ToString();
            if (valueField.FieldType == FieldType.Message)
                schema.MapValue = BuildMessageSchema(valueField.MessageType, depth + 1, comments);
            else
                schema.MapValueType = valueField.FieldType.ToString();
        }

        if (field is { FieldType: FieldType.Message, IsMap: false })
            schema.Message = BuildMessageSchema(field.MessageType, depth + 1, comments);

        if (field.FieldType == FieldType.Enum)
        {
            schema.EnumType = field.EnumType.Name;
            schema.EnumValues = field.EnumType.Values.Select(v => v.Name).ToArray();
        }

        return schema;
    }

    private static IReadOnlyDictionary<string, string> CommentsFor(FileDescriptor file,
        Assembly assembly, ILogger<GrpcServiceCatalog> logger)
    {
        var roots = assembly.Location is { Length: > 0 } location
            ? new[] { Path.GetDirectoryName(location) }
            : null;

        var path = ProtoDocComments.TryResolvePath(file.Name, roots);
        if (path is null)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            return ProtoDocComments.Parse(path);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to parse proto comments from {Path}.", path);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }
}
