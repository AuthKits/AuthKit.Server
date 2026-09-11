namespace DevTools.Runtime;

/// <summary>
/// Request payload for invoking a unary gRPC method.
/// </summary>
/// <remarks>
/// The request message is supplied as JSON and is parsed using the protobuf
/// JSON parser, so enum names, well-known types, and field names are handled
/// the same way as generated clients.
/// </remarks>
public sealed class GrpcInvocationRequest
{
    /// <summary>
    /// Gets the full name of the service (e.g. <c>greet.Greeter</c>).
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// Gets the name of the method to invoke.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the request message serialized as JSON.
    /// </summary>
    public required string RequestJson { get; init; }

    /// <summary>
    /// Gets the additional gRPC metadata (headers) sent with the call.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
}