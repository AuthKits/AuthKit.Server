using AuthKit.Plugins.Abstractions;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Resolves the <see cref="IApiKeyLocationExtractor"/> responsible for a given
/// <see cref="AuthKitApiKeyLocation"/>.
/// </summary>
public interface IApiKeyLocationExtractorRegistry
{
    /// <summary>
    /// Returns the extractor registered for the given location.
    /// </summary>
    /// <param name="location">The credential location to resolve.</param>
    /// <returns>The extractor handling the requested location.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no extractor is registered for the location.
    /// </exception>
    IApiKeyLocationExtractor Resolve(AuthKitApiKeyLocation location);
}