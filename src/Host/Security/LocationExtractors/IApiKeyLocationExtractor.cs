using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key credential from a request for a single
/// <see cref="AuthKitApiKeyLocation"/>.
/// </summary>
public interface IApiKeyLocationExtractor
{
    /// <summary>
    /// Gets the credential location this extractor handles.
    /// </summary>
    AuthKitApiKeyLocation Location { get; }

    /// <summary>
    /// Attempts to extract the API key for the given scheme from the request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="scheme">The security scheme describing where the credential lives.</param>
    /// <returns>The extracted API key, or null when none is present. </returns>
    Task<string?> ExtractAsync(HttpContext context, AuthKitSecuritySchemeDescriptor scheme);
}