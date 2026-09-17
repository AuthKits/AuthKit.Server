namespace DevTools.Runtime;

/// <summary>
/// Result of a single gRPC invocation exposed by the UI API.
/// </summary>
/// <remarks>
/// A successful invocation carries the serialized response in
/// <see cref="ResponseJson"/> and raw protobuf bytes in <see cref="ResponseBase64"/>.
/// Failures carry the gRPC status identifier and code, a human-readable detail,
/// and any response trailers.
/// </remarks>
public sealed class GrpcInvocationResult
{
    /// <summary>
    /// Gets a value indicating whether the invocation completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the gRPC status name of the invocation (e.g. <c>OK</c>).
    /// </summary>
    public required string StatusName { get; init; }

    /// <summary>
    /// Gets the numeric gRPC status code of the invocation.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Gets the human-readable status detail when the invocation failed.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the response message serialized as JSON when the invocation
    /// succeeded.
    /// </summary>
    public string? ResponseJson { get; init; }

    /// <summary>
    /// Gets the raw binary protobuf response encoded as base64 string.
    /// <para>
    /// This field is only populated on successful invocations; for failed
    /// invocations it remains <c>null</c>. The base64-encoded payload is
    /// limited to <c>int.MaxValue - 1</c> bytes (approximately 2 GB) due
    /// to protobuf size constraints.
    /// </para>
    /// </summary>
    public string? ResponseBase64 { get; init; }

    /// <summary>
    /// Gets the invocation duration in milliseconds.
    /// </summary>
    public double ElapsedMs { get; init; }

    /// <summary>
    /// Gets the response trailers returned with the call.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Trailers { get; init; }
}
