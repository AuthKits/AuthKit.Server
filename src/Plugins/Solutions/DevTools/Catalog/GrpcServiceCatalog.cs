using System.Reflection;
using System.Runtime.Loader;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;

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
                        services.Add(BuildServiceInfo(descriptor));
                        foreach (var method in descriptor.Methods)
                            methods[$"{descriptor.FullName}/{method.Name}"] =
                                new GrpcMethodCatalogEntry(BuildMethodInfo(method), method);
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

    private static GrpcServiceInfo BuildServiceInfo(ServiceDescriptor descriptor) =>
        new()
        {
            Name = descriptor.Name,
            FullName = descriptor.FullName,
            Package = descriptor.File.Package,
            FileName = descriptor.File.Name,
            Methods = descriptor.Methods
                .Select(BuildMethodInfo)
                .ToArray()
        };

    private static GrpcMethodInfo BuildMethodInfo(MethodDescriptor method) =>
        new()
        {
            Name = method.Name,
            FullName = $"{method.Service.FullName}/{method.Name}",
            IsClientStreaming = method.IsClientStreaming,
            IsServerStreaming = method.IsServerStreaming,
            Request = BuildMessageSchema(method.InputType, depth: 0),
            Response = BuildMessageSchema(method.OutputType, depth: 0)
        };

    private static GrpcMessageSchema BuildMessageSchema(MessageDescriptor message, int depth) =>
        new()
        {
            Name = message.Name,
            FullName = message.FullName,
            Fields = depth >= MaxMessageDepth
                ? []
                : message.Fields.InFieldNumberOrder()
                    .Select(field => BuildFieldSchema(field, depth))
                    .ToArray()
        };

    private static GrpcFieldSchema BuildFieldSchema(FieldDescriptor field, int depth)
    {
        var schema = new GrpcFieldSchema
        {
            Name = field.Name,
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
                schema.MapValue = BuildMessageSchema(valueField.MessageType, depth + 1);
            else
                schema.MapValueType = valueField.FieldType.ToString();
        }

        if (field is { FieldType: FieldType.Message, IsMap: false })
            schema.Message = BuildMessageSchema(field.MessageType, depth + 1);

        if (field.FieldType == FieldType.Enum)
        {
            schema.EnumType = field.EnumType.Name;
            schema.EnumValues = field.EnumType.Values.Select(v => v.Name).ToArray();
        }

        return schema;
    }
}