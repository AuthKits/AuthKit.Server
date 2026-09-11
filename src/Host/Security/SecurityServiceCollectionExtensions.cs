using AuthKit.Plugins.Abstractions;
using Host.Security.BodyParsers;
using Host.Security.LocationExtractors;
using Host.Security.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Host.Security;

/// <summary>
/// Provides dependency injection configuration for API key credential extraction.
/// </summary>
/// <remarks>
/// <para>
/// Centralizes the registration of credential location strategies and request
/// body parsers used to authenticate API key schemes.
/// </para>
/// <para>
/// New locations or body formats are supported by registering additional
/// <see cref="IApiKeyLocationExtractor"/> or <see cref="IApiKeyBodyParser"/>
/// implementations — no existing code needs to change.
/// </para>
/// </remarks>
public static class SecurityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the API key credential extraction strategies and their options.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// An optional callback used to configure <see cref="ApiKeyCredentialExtractorOptions"/>.
    /// </param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddApiKeyCredentialExtraction(
        this IServiceCollection services,
        Action<ApiKeyCredentialExtractorOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));

        services.AddSingleton<ISecuritySchemeRegistry, SecuritySchemeRegistry>();
        services.AddSingleton<IApiKeyLocationExtractorRegistry, ApiKeyLocationExtractorRegistry>();

        services.AddSingleton<IApiKeyLocationExtractor, HeaderApiKeyLocationExtractor>();
        services.AddSingleton<IApiKeyLocationExtractor, QueryApiKeyLocationExtractor>();
        services.AddSingleton<IApiKeyLocationExtractor, CookieApiKeyLocationExtractor>();
        services.AddSingleton<IApiKeyLocationExtractor, GrpcMetadataApiKeyLocationExtractor>();
        services.AddSingleton<IApiKeyLocationExtractor, BodyApiKeyLocationExtractor>();

        services.AddSingleton<IApiKeyBodyParser, JsonApiKeyBodyParser>();
        services.AddSingleton<IApiKeyBodyParser, FormUrlEncodedApiKeyBodyParser>();

        return services;
    }
}