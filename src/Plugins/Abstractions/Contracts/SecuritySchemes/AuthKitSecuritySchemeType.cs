namespace AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

/// <summary>
/// Specifies the authentication mechanism represented by an AuthKit security scheme.
/// </summary>
/// <remarks>
/// <para>
/// The selected value determines how the corresponding
/// <see cref="AuthKitSecuritySchemeDescriptor"/> describes the credentials
/// and authentication flow exposed by an AuthKit plugin.
/// </para>
/// <para>
/// This enumeration describes the authentication mechanism at the metadata
/// level. It does not itself perform authentication, validate credentials,
/// or establish an authentication session.
/// </para>
/// <para>
/// Numeric values are part of the plugin contract and MUST NOT be reused or
/// renumbered. Each value must remain unique so an authentication mechanism
/// can never be confused with a different mechanism that happens to share a
/// value.
/// </para>
/// </remarks>
public enum AuthKitSecuritySchemeType
{
    /// <summary>
    /// Authentication using an API key supplied through a configured location.
    /// </summary>
    /// <remarks>
    /// The credential location is specified by
    /// <see cref="AuthKitSecuritySchemeDescriptor.In"/>.
    /// Common locations include an HTTP header, query parameter, or cookie.
    /// </remarks>
    ApiKey = 0,

    /// <summary>
    /// Authentication using an HTTP authentication scheme.
    /// </summary>
    /// <remarks>
    /// The HTTP authentication scheme is specified by
    /// <see cref="AuthKitSecuritySchemeDescriptor.Scheme"/>.
    /// Examples include <c>Basic</c>, <c>Bearer</c>, and other HTTP
    /// authentication schemes.
    /// </remarks>
    Http = 1,

    /// <summary>
    /// Authentication using the OAuth 2.0 authorization framework.
    /// </summary>
    /// <remarks>
    /// OAuth 2.0 schemes describe authorization flows in which a client
    /// gets an access token from an authorization server and presents
    /// that token when accessing protected resources.
    /// </remarks>
    OAuth2 = 2,

    /// <summary>
    /// Mutual TLS authentication using client certificate.
    /// </summary>
    MutualTls = 3,

    /// <summary>
    /// Session-based authentication.
    /// A cookie may be used as transport mechanism but does not define the authentication mechanism itself.
    /// </summary>
    Session = 4,

    /// <summary>
    /// A plugin defined authentication mechanism that is not covered by the built-in security scheme types.
    /// </summary>
    Custom = 5,

    /// <summary>
    /// HTTP Basic authentication using the Authorization header.
    /// </summary>
    Basic = 6,

    /// <summary>
    /// Authentication using the OpenID Connect identity layer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OpenID Connect extends OAuth 2.0 with an identity layer and is used
    /// to authenticate users through an OpenID Connect identity provider.
    /// </para>
    /// <para>
    /// The value <c>7</c> is intentional: the host already occupies values
    /// <c>0</c> (<see cref="ApiKey"/>), <c>1</c> (<see cref="Http"/>), and
    /// <c>2</c> (<see cref="OAuth2"/>), while values <c>3</c>..<c>6</c> belong
    /// to <see cref="MutualTls"/>, <see cref="Session"/>, <see cref="Custom"/>,
    /// and <see cref="Basic"/>. Historically <see cref="OpenIdConnect"/>
    /// aliased <see cref="OAuth2"/> at value <c>2</c>; it now has a distinct
    /// value so OAuth 2.0 and OpenID Connect can never be confused.
    /// </para>
    /// </remarks>
    OpenIdConnect = 7
}
