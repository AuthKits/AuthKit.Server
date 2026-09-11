using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Microsoft.Extensions.Options;

using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key from an HTTP request query parameter.
/// </summary>
/// <remarks>
/// The query parameter name is resolved from the scheme's
/// <see cref="AuthKitSecuritySchemeDescriptor.CredentialName"/> and falls back
/// to <see cref="ApiKeyCredentialExtractorOptions.DefaultQueryName"/> when no
/// explicit credential name is provided.
/// </remarks>
public sealed class QueryApiKeyLocationExtractor(
    IOptions<ApiKeyCredentialExtractorOptions> options)
    : ApiKeyLocationExtractorBase(options)
{
    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public override AuthKitApiKeyLocation Location =>
        AuthKitApiKeyLocation.Query;

    /// <summary>
    /// Extracts an API key from the specified request query parameter.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>The normalized API key, or null if the query parameter is not present.</returns>
    public override Task<string?> ExtractAsync(
        HttpContext context,
        AuthKitSecuritySchemeDescriptor scheme)
    {
        var queryName = ResolveName(scheme.CredentialName, Options.DefaultQueryName);

        if (context.Request.Query.TryGetValue(queryName, out var query))
        {
            return Task.FromResult<string?>(
                ApiKeyValueNormalizer.Normalize(query.ToString()));
        }

        return Task.FromResult<string?>(null);
    }
}