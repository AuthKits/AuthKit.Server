namespace DevTools.Catalog;

/// <summary>
/// Descriptor of a protobuf scalar or composite field in a request or
/// response message.
/// </summary>
/// <remarks>
/// Only one of the composite members is populated for a given field:
/// <see cref="Message"/> for nested messages, <see cref="MapValue"/> for
/// message values of a map, or <see cref="EnumType"/> plus
/// <see cref="EnumValues"/> for enums.
/// </remarks>
public sealed class GrpcFieldSchema
{
    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets the protobuf field type (e.g. <c>String</c>, <c>Int32</c>, <c>Message</c>).
    /// </summary>
    public required string FieldType { get; set; }

    /// <summary>
    /// Gets a value indicating whether the field is repeated.
    /// </summary>
    public bool IsRepeated { get; set; }

    /// <summary>
    /// Gets a value indicating whether the field is a map.
    /// </summary>
    public bool IsMap { get; set; }

    /// <summary>
    /// Gets the key type of a map field.
    /// </summary>
    public string? MapKeyType { get; set; }

    /// <summary>
    /// Gets the scalar value type of a map field.
    /// </summary>
    public string? MapValueType { get; set; }

    /// <summary>
    /// Gets the message value schema of a map field.
    /// </summary>
    public GrpcMessageSchema? MapValue { get; set; }

    /// <summary>
    /// Gets the nested message schema of a message field.
    /// </summary>
    public GrpcMessageSchema? Message { get; set; }

    /// <summary>
    /// Gets the enum type name of an enum field.
    /// </summary>
    public string? EnumType { get; set; }

    /// <summary>
    /// Gets the enum values of an enum field.
    /// </summary>
    public IEnumerable<string>? EnumValues { get; set; }
}

/// <summary>
/// Protobuf message layout used to render the request and response schema
/// in the UI.
/// </summary>
/// <remarks>
/// Nested messages are expanded up to a fixed depth to avoid infinite
/// recursion on self-referencing messages.
/// </remarks>
public sealed class GrpcMessageSchema
{
    /// <summary>
    /// Gets the short name of the message.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the fully qualified name of the message.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the fields of the message in field number order.
    /// </summary>
    public required IEnumerable<GrpcFieldSchema> Fields { get; init; }
}

/// <summary>
/// Descriptor of a single RPC method including its input and output schemas.
/// </summary>
public sealed class GrpcMethodInfo
{
    /// <summary>
    /// Gets the name of the method.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the fully qualified method name (e.g. <c>greet.Greeter/SayHello</c>).
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets a value indicating whether the method is a client-streaming method.
    /// </summary>
    public bool IsClientStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether the method is a server-streaming method.
    /// </summary>
    public bool IsServerStreaming { get; init; }

    /// <summary>
    /// Gets the schema of the method's input message.
    /// </summary>
    public required GrpcMessageSchema Request { get; init; }

    /// <summary>
    /// Gets the schema of the method's output message.
    /// </summary>
    public required GrpcMessageSchema Response { get; init; }
}

/// <summary>
/// Descriptor of a gRPC service together with all of its methods.
/// </summary>
public sealed class GrpcServiceInfo
{
    /// <summary>
    /// Gets the short name of the service.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the fully qualified name of the service.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets the protobuf package the service belongs to.
    /// </summary>
    public required string Package { get; init; }

    /// <summary>
    /// Gets the name of the .proto file defining the service.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the methods exposed by the service.
    /// </summary>
    public required IEnumerable<GrpcMethodInfo> Methods { get; init; }
}

/// <summary>
/// Payload returned by the catalog API endpoint.
/// </summary>
public sealed class GrpcCatalogResponse
{
    /// <summary>
    /// Gets the gRPC endpoint used to execute invocations.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Gets the discovered gRPC services.
    /// </summary>
    public required IEnumerable<GrpcServiceInfo> Services { get; init; }
}