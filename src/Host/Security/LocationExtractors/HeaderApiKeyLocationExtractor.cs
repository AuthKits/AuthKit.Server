using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Microsoft.Extensions.Options;

using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key from an HTTP request header or transport metadata.
/// </summary>
/// <remarks>
/// <para>
/// The header name is resolved from the scheme's <see cref="AuthKitSecuritySchemeDescriptor.CredentialName"/>
/// and falls back to <see cref="ApiKeyCredentialExtractorOptions.DefaultHeaderName"/> when no
/// explicit credential name is provided.
/// </para>
/// <para>
/// When <see cref="ApiKeyCredentialExtractorOptions.StripBearerPrefix"/> is enabled, a
/// case-insensitive Bearer prefix is removed from the extracted value, allowing the header
/// to contain either a plain API key or a bearer token.
/// </para>
/// </remarks>
public sealed class HeaderApiKeyLocationExtractor(
    IOptions<ApiKeyCredentialExtractorOptions> options)
    : ApiKeyLocationExtractorBase(options)
{
    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public override AuthKitApiKeyLocation Location =>
        AuthKitApiKeyLocation.Header;

    /// <summary>
    /// Extracts an API key from the specified request header.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>
    /// The normalized API key, or null if the specified header is not present.
    /// </returns>
    public override Task<string?> ExtractAsync(
        HttpContext context,
        AuthKitSecuritySchemeDescriptor scheme)
    {
        var headerName = ResolveName(scheme.CredentialName, Options.DefaultHeaderName);

        if (context.Request.Headers.TryGetValue(headerName, out var header))
        {
            var value = header.ToString();
            return Task.FromResult<string?>(
                Options.StripBearerPrefix
                    ? ApiKeyValueNormalizer.StripBearerPrefix(value)
                    : ApiKeyValueNormalizer.Normalize(value));
        }

        return Task.FromResult<string?>(null);
    }
}