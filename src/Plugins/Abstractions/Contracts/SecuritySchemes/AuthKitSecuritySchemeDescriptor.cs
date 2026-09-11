namespace AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

/// <summary>
/// Describes a security scheme exposed by an AuthKit authentication plugin.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AuthKitSecuritySchemeDescriptor"/> provides transport-agnostic
/// metadata describing how a client authenticates when communicating with
/// service protected by AuthKit.
/// </para>
/// <para>
/// Depending on <see cref="Type"/>, the descriptor can represent authentication
/// mechanisms such as API keys, HTTP authentication schemes, or bearer tokens.
/// </para>
/// <para>
/// For API key schemes, <see cref="In"/> determines where AuthKit should look
/// for the credential. The selected location may apply differently depending
/// on the underlying transport, such as HTTP or gRPC.
/// </para>
/// </remarks>
public sealed record AuthKitSecuritySchemeDescriptor
{
    /// <summary>
    /// The unique name used to identify the security scheme.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Name"/> is a transport-agnostic scheme identity. It is used in logs,
    /// validation messages, and as the scheme key in OpenAPI documents. It must not be
    /// reused as the transport credential field name.
    /// </para>
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// The transport-specific field name used to locate the credential, such as the
    /// header, query parameter, cookie, gRPC metadata key, or body field name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CredentialName"/> is independent from <see cref="Name"/>: a scheme can
    /// be identified as <c>DevTokens</c> while its credential travels as an
    /// <c>X-Api-Key</c> header. When not set, hosts fall back to their configured default
    /// field name for the selected <see cref="In"/> location.
    /// </para>
    /// </remarks>
    public string? CredentialName { get; init; }

    /// <summary>
    /// The type of authentication mechanism implemented by the security scheme.
    /// </summary>
    public required AuthKitSecuritySchemeType Type { get; init; }

    /// <summary>
    /// Specifies where the API key should be retrieved from when
    /// <see cref="Type"/> represents an API key authentication scheme.
    /// </summary>
    public AuthKitApiKeyLocation In { get; init; }

    /// <summary>
    /// The authentication scheme name.
    /// </summary>
    /// <remarks>
    /// For HTTP authentication schemes, this can specify the scheme used
    /// by the security mechanism, such as <c>Bearer</c>.
    /// </remarks>
    public string? Scheme { get; init; }

    /// <summary>
    /// An optional hint describing the format of a bearer token.
    /// </summary>
    /// <remarks>
    /// This value is intended primarily for documentation and client
    /// generation purposes and does not affect token validation.
    /// </remarks>
    public string? BearerFormat { get; init; }

    /// <summary>
    /// An optional description of the security scheme.
    /// </summary>
    public string? Description { get; init; }
}