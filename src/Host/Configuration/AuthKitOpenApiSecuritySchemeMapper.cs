using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Host.Security.Options;
using Microsoft.OpenApi;

namespace Host.Configuration;

/// <summary>
/// Maps AuthKit security scheme descriptors to OpenAPI security scheme definitions.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="AuthKitSecuritySchemeType"/> value is handled explicitly. Values that have a
/// semantically correct OpenAPI representation are mapped to the OpenAPI security scheme type of the
/// document's spec version; values that cannot be represented by the configured OpenAPI version — or
/// whose semantics require host functionality that the current host does not implement — are rejected
/// with a descriptive exception. No value is ever silently mapped to a generic or unrelated security
/// scheme.
/// </para>
/// <para>
/// The current host serializes OpenAPI documents (Swashbuckle 10.2.x / Microsoft.OpenApi 2.7.x).
/// That stack exposes both <see cref="SecuritySchemeType.MutualTLS"/> and OpenAPI 3.1 serialization
/// (<c>SwaggerOptions.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1</c>), so
/// <see cref="AuthKitSecuritySchemeType.MutualTls"/> is represented natively — but only in
/// documents pinned to OpenAPI 3.1. Under OpenAPI 3.0 (the host default), <c>mutualTLS</c> has no
/// representation and mutual-TLS schemes are still explicitly rejected rather than rewritten as an
/// HTTP, bearer, or API key scheme.
/// </para>
/// <para>
/// API key locations <see cref="AuthKitApiKeyLocation.GrpcMetadata"/> and
/// <see cref="AuthKitApiKeyLocation.Body"/> are valid credential transports supported by this host at
/// runtime, but no OpenAPI version (3.0 or 3.1) can describe gRPC metadata or body locations. They
/// must never be silently rewritten to header, query, or cookie locations.
/// </para>
/// <para>
/// <see cref="AuthKitSecuritySchemeType.Session"/> and <see cref="AuthKitSecuritySchemeType.Custom"/>
/// and any unknown (future) <see cref="AuthKitSecuritySchemeType"/> value are similarly rejected.
/// No generic or unrelated scheme is ever produced as a fallback.
/// </para>
/// </remarks>
public static class AuthKitOpenApiSecuritySchemeMapper
{
    /// <summary>
    /// Maps the supplied descriptor to an <see cref="OpenApiSecurityScheme"/>.
    /// </summary>
    /// <param name="descriptor">The AuthKit security scheme descriptor to map.</param>
    /// <param name="specVersion">The OpenAPI spec version of the document being generated.</param>
    /// <returns>The equivalent OpenAPI security scheme definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="descriptor"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">
    /// The descriptor declares a scheme type or API key location that has no semantically correct
    /// OpenAPI representation in the configured spec version.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The descriptor declares an unknown (future) <see cref="AuthKitSecuritySchemeType"/> or
    /// <see cref="AuthKitApiKeyLocation"/> value.
    /// </exception>
    public static OpenApiSecurityScheme Map(
        AuthKitSecuritySchemeDescriptor descriptor,
        OpenApiSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var scheme = new OpenApiSecurityScheme
        {
            Name = ResolveCredentialName(descriptor),
            BearerFormat = descriptor.BearerFormat,
            Description = descriptor.Description
        };

        switch (descriptor.Type)
        {
            case AuthKitSecuritySchemeType.ApiKey:
                scheme.Type = SecuritySchemeType.ApiKey;
                scheme.In = MapApiKeyLocation(descriptor, specVersion);
                break;

            case AuthKitSecuritySchemeType.Http:
                scheme.Type = SecuritySchemeType.Http;
                scheme.Scheme = descriptor.Scheme;
                break;

            case AuthKitSecuritySchemeType.OAuth2:
                scheme.Type = SecuritySchemeType.OAuth2;
                break;

            case AuthKitSecuritySchemeType.OpenIdConnect:
                scheme.Type = SecuritySchemeType.OpenIdConnect;
                break;

            case AuthKitSecuritySchemeType.Basic:
                scheme.Type = SecuritySchemeType.Http;
                scheme.Scheme = "basic";
                break;

            case AuthKitSecuritySchemeType.MutualTls:
                if (specVersion == OpenApiSpecVersion.OpenApi3_1)
                {
                    scheme.Type = SecuritySchemeType.MutualTLS;
                }
                else
                {
                    throw new NotSupportedException(
                        $"Security scheme '{descriptor.Name}' uses AuthKitSecuritySchemeType.MutualTls, " +
                        $"which cannot be represented in the configured OpenAPI {SpecVersionToLabel(specVersion)} " +
                        $"document. The current host (Swashbuckle 10.2.x / Microsoft.OpenApi 2.7.x) can serialize " +
                        $"mutual TLS natively only when the document is pinned to OpenAPI 3.1 " +
                        $"(OpenApi:SpecVersion = 3.1). Under {SpecVersionToLabel(specVersion)} it must be rejected " +
                        $"rather than rewritten as an HTTP, bearer, or API key scheme.");
                }
                break;

            case AuthKitSecuritySchemeType.Session:
                throw new NotSupportedException(
                    $"Security scheme '{descriptor.Name}' uses AuthKitSecuritySchemeType.Session, which has no " +
                    $"semantically correct OpenAPI security scheme representation in any OpenAPI version. " +
                    $"Session authentication must not be described as an API key, HTTP, or bearer scheme.");

            case AuthKitSecuritySchemeType.Custom:
                throw new NotSupportedException(
                    $"Security scheme '{descriptor.Name}' uses AuthKitSecuritySchemeType.Custom, which has no " +
                    $"built-in OpenAPI security scheme representation. Hosts must register an explicit " +
                    $"mapping for custom schemes; no default generic mapping is applied.");

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(descriptor),
                    $"Unknown AuthKitSecuritySchemeType value '{descriptor.Type}' for security scheme " +
                    $"'{descriptor.Name}' must be rejected rather than mapped to a generic scheme.");
        }

        return scheme;
    }

    private static ParameterLocation MapApiKeyLocation(
        AuthKitSecuritySchemeDescriptor descriptor,
        OpenApiSpecVersion specVersion) =>
        descriptor.In switch
        {
            AuthKitApiKeyLocation.Header => ParameterLocation.Header,
            AuthKitApiKeyLocation.Query => ParameterLocation.Query,
            AuthKitApiKeyLocation.Cookie => ParameterLocation.Cookie,

            AuthKitApiKeyLocation.GrpcMetadata => throw new NotSupportedException(
                $"Security scheme '{descriptor.Name}' uses AuthKitApiKeyLocation.GrpcMetadata, which has no " +
                $"OpenAPI representation in version {SpecVersionToLabel(specVersion)}. " +
                $"It must not be silently mapped to a header location."),

            AuthKitApiKeyLocation.Body => throw new NotSupportedException(
                $"Security scheme '{descriptor.Name}' uses AuthKitApiKeyLocation.Body, which has no " +
                $"OpenAPI representation in any OpenAPI version. It must not be silently mapped to a header, " +
                $"query, or cookie location."),

            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                $"Unknown AuthKitApiKeyLocation value '{descriptor.In}' for security scheme " +
                $"'{descriptor.Name}' must be rejected rather than mapped to another location.")
        };

    private static string SpecVersionToLabel(OpenApiSpecVersion specVersion) =>
        specVersion switch
        {
            OpenApiSpecVersion.OpenApi2_0 => "2.0 (Swagger)",
            OpenApiSpecVersion.OpenApi3_0 => "3.0",
            OpenApiSpecVersion.OpenApi3_1 => "3.1",
            _ => specVersion.ToString()
        };

    /// <summary>
    /// Resolves the credential field name for the OpenAPI <c>name</c> property
    /// from the descriptor's explicit <see cref="AuthKitSecuritySchemeDescriptor.CredentialName"/>
    /// or the host's location-specific default.
    /// </summary>
    private static string ResolveCredentialName(AuthKitSecuritySchemeDescriptor descriptor) =>
        descriptor.CredentialName
        ?? descriptor.In switch
        {
            AuthKitApiKeyLocation.Header => ApiKeyCredentialExtractorOptions.DefaultHeaderNameValue,
            AuthKitApiKeyLocation.Query  => ApiKeyCredentialExtractorOptions.DefaultQueryNameValue,
            AuthKitApiKeyLocation.Cookie => ApiKeyCredentialExtractorOptions.DefaultCookieNameValue,
            _ => descriptor.Name
        };
}
