using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Microsoft.Extensions.Options;

using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key from an HTTP request cookie.
/// </summary>
/// <remarks>
/// <para>
/// The cookie name is resolved from the scheme's
/// <see cref="AuthKitSecuritySchemeDescriptor.CredentialName"/> and falls back to
/// <see cref="ApiKeyCredentialExtractorOptions.DefaultCookieName"/> when no
/// explicit credential name is provided.
/// </para>
/// </remarks>
public sealed class CookieApiKeyLocationExtractor(
    IOptions<ApiKeyCredentialExtractorOptions> options)
    : ApiKeyLocationExtractorBase(options)
{
    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public override AuthKitApiKeyLocation Location =>
        AuthKitApiKeyLocation.Cookie;

    /// <summary>
    /// Extracts an API key from the request cookie collection.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>
    /// The normalized API key, or null if the specified cookie is not present.
    /// </returns>
    public override Task<string?> ExtractAsync(
        HttpContext context,
        AuthKitSecuritySchemeDescriptor scheme)
    {
        var cookieName = ResolveName(scheme.CredentialName, Options.DefaultCookieName);

        if (context.Request.Cookies.TryGetValue(cookieName, out var cookie))
        {
            return Task.FromResult<string?>(
                ApiKeyValueNormalizer.Normalize(cookie));
        }

        return Task.FromResult<string?>(null);
    }
}