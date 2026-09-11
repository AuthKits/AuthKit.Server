using System.Text;
using AuthKit.Plugins.Abstractions;
using Microsoft.Extensions.Options;
using Host.Security.BodyParsers;
using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Extracts an API key from buffered request body using registered
/// <see cref="IApiKeyBodyParser"/> that supports the request content type.
/// </summary>
/// <remarks>
/// <para>
/// The request body is buffered to allow reading and rewinding without
/// affecting downstream middleware or request handlers.
/// </para>
/// <para>
/// The body is read only when a compatible parser is registered and the
/// request size does not exceed the configured buffer threshold.
/// </para>
/// </remarks>
public sealed class BodyApiKeyLocationExtractor(
    IEnumerable<IApiKeyBodyParser> parsers,
    IOptions<ApiKeyCredentialExtractorOptions> options,
    ILogger<BodyApiKeyLocationExtractor> logger) : ApiKeyLocationExtractorBase(options)
{
    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public override AuthKitApiKeyLocation Location =>
        AuthKitApiKeyLocation.Body;

    /// <summary>
    /// Extracts an API key from the request body using a parser that supports
    /// the request content type.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>
    /// The extracted API key, or null when the body cannot be processed
    /// or does not contain the configured credential field.
    /// </returns>
    public override async Task<string?> ExtractAsync(
        HttpContext context,
        AuthKitSecuritySchemeDescriptor scheme)
    {
        if (context.Request.ContentLength == 0)
            return null;

        var parser = parsers.FirstOrDefault(
            p => p.CanHandle(context.Request.ContentType));

        if (parser is null)
        {
            logger.LogDebug("Skipping Body credential extraction for scheme {Scheme}: unsupported content type {ContentType}",
                scheme.Name,
                context.Request.ContentType);

            return null;
        }

        if (context.Request.ContentLength is { } length
            && length > Options.BufferThreshold)
        {
            logger.LogWarning("Skipping Body credential extraction for scheme {Scheme}: body of {Length} bytes exceeds configured buffer threshold of {Threshold} bytes",
                scheme.Name,
                length,
                Options.BufferThreshold);

            return null;
        }

        if (!context.Request.Body.CanSeek)
            context.Request.EnableBuffering(Options.BufferThreshold);

        var originalPosition = context.Request.Body.Position;

        try
        {
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return null;

            var fieldName = ResolveName(
                scheme.Name,
                Options.DefaultQueryName);

            return parser.Parse(body, fieldName);
        }
        finally
        {
            context.Request.Body.Position = originalPosition;
        }
    }
}