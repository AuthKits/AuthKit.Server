namespace AuthKit.Plugins.Abstractions;

/// <summary>
/// Specifies the transport location from which an API key should be retrieved.
/// </summary>
/// <remarks>
/// <para>
/// The API key can be supplied through different metadata locations depending
/// on the transport used by the incoming request, such as HTTP or gRPC.
/// </para>
/// <para>
    /// For HTTP requests, API keys may be provided through request headers, query
    /// string parameters, or cookies. For gRPC requests, API keys are typically
    /// provided through gRPC request metadata.
    /// </para>
    /// <para>
    /// Every value describes a separate transport. In particular,
    /// <see cref="GrpcMetadata"/> is explicit gRPC metadata and is distinct from
    /// <see cref="Header"/>; a host must never silently convert one to the other.
    /// </para>
/// <para>
/// The selected location determines where the AuthKit authentication pipeline
/// searches for the API key before attempting validation.
/// </para>
/// </remarks>
public enum AuthKitApiKeyLocation
{
    /// <summary>
    /// Retrieves the API key from request headers or transport metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For HTTP requests, the API key is retrieved from configured request
    /// header, such as <c>X-Api-Key</c>.
    /// </para>
    /// <para>
    /// For gRPC requests, the API key may also be retrieved from the request
    /// metadata for backward-compatible plugins. New gRPC configurations should
    /// prefer the explicit <see cref="GrpcMetadata"/> location. This value
    /// remains distinct from <see cref="GrpcMetadata"/>: a host must never
    /// silently convert <see cref="GrpcMetadata"/> to <see cref="Header"/>.
    /// </para>
    /// </remarks>
    Header,

    /// <summary>
    /// Retrieves the API key from request query parameter.
    /// </summary>
    /// <remarks>
    /// The API key is expected to be supplied as part of the HTTP request URL,
    /// for example <c>?api_key=...</c>.
    /// </remarks>
    /// <para>
    /// This location is only applicable to HTTP-based requests and is not
    /// available for native gRPC calls.
    /// </para>
    Query,

    /// <summary>
    /// Retrieves the API key from a request cookie.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The API key is expected to be stored in an HTTP cookie sent with the
    /// incoming request.
    /// </para>
    /// <para>
    /// This location is primarily intended for HTTP browser-based scenarios
    /// and is not available for native gRPC calls.
    /// </para>
    /// </remarks>
    Cookie,

    /// <summary>
    /// Retrieves the API key from gRPC metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The API key is expected to be supplied through gRPC request metadata
    /// (key-value pairs in the gRPC metadata headers).
    /// </para>
    /// <para>
    /// This location is distinct from <see cref="Header"/> and is specifically
    /// for gRPC transport. It must not be silently treated as equivalent to
    /// <see cref="Header"/> even though both use metadata-like structures.
    /// </para>
    /// <para>
    /// A host that does not support gRPC metadata credential extraction must
    /// explicitly reject this configuration.
    /// </para>
    /// </remarks>
    GrpcMetadata = 3,

    /// <summary>
    /// Retrieves the API key from the request body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The API key is expected to be supplied within the request body payload.
    /// The exact format (e.g., JSON field, form field) is determined by the
    /// host implementation.
    /// </para>
    /// <para>
    /// This location is distinct from <see cref="Header"/>, <see cref="Query"/>,
    /// and <see cref="Cookie"/>. It must not be silently converted to any
    /// other location.
    /// </para>
    /// <para>
    /// A host that does not support body-based credential extraction must
    /// explicitly reject this configuration.
    /// </para>
    /// <para>
    /// This value defines only the credential transport location. It does not
    /// define or change the authentication mechanism. For example, when used
    /// with <see cref="AuthKitSecuritySchemeType.ApiKey"/>, it means the API
    /// key credential is transported through the request body.
    /// </para>
    /// </remarks>
    Body = 4
}
