using AuthKit.Plugins.Abstractions;
using Microsoft.Extensions.Options;

using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key from gRPC request metadata represented as HTTP/2 headers.
/// </summary>
/// <remarks>
/// <para>
/// gRPC metadata keys are transmitted as lowercase HTTP/2 headers. The
/// configured credential name is therefore normalized to lowercase before
/// looking it up in the request headers.
/// </para>
/// <para>
/// Hosts that cannot represent gRPC metadata as HTTP headers should reject the
/// configuration rather than silently falling back to another location.
/// </para>
/// </remarks>
public sealed class GrpcMetadataApiKeyLocationExtractor(
    IOptions<ApiKeyCredentialExtractorOptions> options,
    ILogger<GrpcMetadataApiKeyLocationExtractor> logger)
    : ApiKeyLocationExtractorBase(options)
{
    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public override AuthKitApiKeyLocation Location =>
        AuthKitApiKeyLocation.GrpcMetadata;

    /// <summary>
    /// Extracts an API key from gRPC request metadata represented as an HTTP/2 header.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>The normalized API key, or null if the metadata entry is not present.</returns>
    public override Task<string?> ExtractAsync(
        HttpContext context,
        AuthKitSecuritySchemeDescriptor scheme)
    {
        var headerName = string.IsNullOrWhiteSpace(scheme.Name)
            ? Options.DefaultHeaderName.ToLowerInvariant()
            : scheme.Name.ToLowerInvariant();

        if(headerName == Options.DefaultHeaderName.ToLowerInvariant())
        {
            logger.LogDebug(
                "Reading gRPC metadata for scheme {Scheme} as HTTP header '{HeaderName}'",
                scheme.Name,
                headerName);
        }

        if(context.Request.Headers.TryGetValue(headerName, out var header))
        {
            return Task.FromResult<string?>(
                ApiKeyValueNormalizer.Normalize(header.ToString()));
        }

        return Task.FromResult<string?>(null);
    }
}