using System.Text;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
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
/// <see cref="ApiKeyCredentialExtractorOptions.BufferThreshold"/> controls how much of the
/// body is buffered in memory before spilling to a temporary file. It is not a size limit:
/// bodies larger than the threshold continue to be read.
/// </para>
/// <para>
/// <see cref="ApiKeyCredentialExtractorOptions.MaxBodySize"/> is the optional hard limit on
/// how much of the request body is read for credential extraction. When set, bodies larger
/// than the limit are rejected instead of being read in full. Requests without a known
/// <c>Content-Length</c> are read through the same bounded path when <see cref="ApiKeyCredentialExtractorOptions.MaxBodySize"/>
/// is configured.
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
            && Options.MaxBodySize > 0
            && length > Options.MaxBodySize)
        {
            logger.LogWarning(
                "Skipping Body credential extraction for scheme {Scheme}: body of {Length} bytes exceeds configured MaxBodySize of {MaxBodySize} bytes",
                scheme.Name,
                length,
                Options.MaxBodySize);

            return null;
        }

        if (!context.Request.Body.CanSeek)
            context.Request.EnableBuffering(Options.BufferThreshold);

        var originalPosition = context.Request.Body.Position;

        try
        {
            context.Request.Body.Position = 0;

            string? body;

            if (Options.MaxBodySize > 0)
            {
                body = await ReadBoundedBodyAsync(context.Request.Body, Options.MaxBodySize);
            }
            else
            {
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    leaveOpen: true);

                body = await reader.ReadToEndAsync();
            }

            if (body is null)
            {
                logger.LogWarning(
                    "Skipping Body credential extraction for scheme {Scheme}: body exceeds configured MaxBodySize of {MaxBodySize} bytes",
                    scheme.Name,
                    Options.MaxBodySize);

                return null;
            }

            if (string.IsNullOrWhiteSpace(body))
                return null;

            var fieldName = ResolveName(
                scheme.CredentialName,
                Options.DefaultQueryName);

            return parser.Parse(body, fieldName);
        }
        finally
        {
            context.Request.Body.Position = originalPosition;
        }
    }

    /// <summary>
    /// Reads up to <paramref name="maxBytes"/> bytes from the body and decodes them as UTF-8.
    /// Returns null when the body is larger than the limit.
    /// </summary>
    private static async Task<string?> ReadBoundedBodyAsync(Stream body, long maxBytes)
    {
        var buffer = new byte[4096];

        using var ms = new MemoryStream();

        long total = 0;
        int read;
        while ((read = await body.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            total += read;

            if (total > maxBytes)
                return null;

            ms.Write(buffer, 0, read);
        }

        ms.Position = 0;

        using var reader = new StreamReader(
            ms,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false);

        return await reader.ReadToEndAsync();
    }
}