using AuthKit.Plugins.Abstractions;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Resolves API key location extractors registered in the dependency injection container.
/// </summary>
/// <remarks>
/// <para>
/// Extractors are indexed by their declared <see cref="AuthKitApiKeyLocation"/>.
/// Registering more than one extractor for the same location causes an exception.
/// </para>
/// <para>
/// Additional locations can be supported by implementing
/// <see cref="IApiKeyLocationExtractor"/> and registering the implementation
/// in the dependency injection container.
/// </para>
/// </remarks>
public sealed class ApiKeyLocationExtractorRegistry(
    IEnumerable<IApiKeyLocationExtractor> extractors) : IApiKeyLocationExtractorRegistry
{
    private readonly IReadOnlyDictionary<AuthKitApiKeyLocation, IApiKeyLocationExtractor> _extractors =
        extractors.ToDictionary(e => e.Location);

    /// <summary>
    /// Resolves the extractor registered for the specified API key location.
    /// </summary>
    /// <param name="location">The API key credential location.</param>
    /// <returns>The extractor responsible for the specified location.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when no extractor is registered for the specified location.
    /// </exception>
    public IApiKeyLocationExtractor Resolve(AuthKitApiKeyLocation location) =>
        _extractors.TryGetValue(location, out var extractor)
            ? extractor
            : throw new NotSupportedException(
                $"No API key credential extractor is registered for location '{location}'. Configure the host to support this location.");
}