using AuthKit.Plugins.Abstractions;

namespace Host.Security.Options;

/// <summary>
/// Options for configuring API key credential extraction.
/// </summary>
/// <remarks>
/// <para>
/// Default names are used by the location extractors whenever the declaring
/// scheme does not specify an explicit credential name.
/// </para>
/// <para>
/// <see cref="BufferThreshold"/> controls how much of the request body is
/// buffered in memory before it spills to a temporary file. It is not a hard
/// size limit — see <see cref="MaxBodySize"/> for the optional extraction limit.
/// </para>
/// </remarks>
public sealed class ApiKeyCredentialExtractorOptions
{
    /// <summary>
    /// Default value for <see cref="DefaultHeaderName"/>.
    /// </summary>
    public const string DefaultHeaderNameValue = "X-Api-Key";

    /// <summary>
    /// Default value for <see cref="DefaultQueryName"/>.
    /// </summary>
    public const string DefaultQueryNameValue = "api_key";

    /// <summary>
    /// Default value for <see cref="DefaultCookieName"/>.
    /// </summary>
    public const string DefaultCookieNameValue = "api_key";

    /// <summary>
    /// Gets or sets the memory threshold, in bytes, used when buffering the
    /// request body for <see cref="AuthKitApiKeyLocation.Body"/> extraction.
    /// Bodies larger than this value spill to a temporary file (default:
    /// 1 MB).
    /// </summary>
    public long BufferThreshold { get; set; } = 1_048_576; // 1 MB

    /// <summary>
    /// Gets or sets the maximum request body size, in bytes, read for
    /// <see cref="AuthKitApiKeyLocation.Body"/> credential extraction.
    /// Bodies larger than this value are rejected instead of being read in
    /// full. Requests without a known <c>Content-Length</c> are read through
    /// the same bounded path. A value of <c>0</c> disables the limit and reads
    /// the entire body that fits within <see cref="BufferThreshold"/> spill
    /// behavior (default: <c>0</c>, no limit).
    /// </summary>
    public long MaxBodySize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a case-insensitive
    /// <c>Bearer </c> prefix is removed from values extracted from the
    /// <see cref="AuthKitApiKeyLocation.Header"/> before validation (default:
    /// <c>false</c>).
    /// </summary>
    /// <remarks>
    /// When disabled, headers are normalized only (whitespace and wrapping
    /// quotes are trimmed) and a <c>Bearer </c> prefix is passed through to the
    /// validator unchanged. Enable this option only for schemes where callers
    /// are expected to share the header between plain API keys and bearer
    /// tokens.
    /// </remarks>
    public bool StripBearerPrefix { get; set; }

    /// <summary>
    /// Gets or sets the header name to use when the scheme does not specify
    /// one (default: <c>X-Api-Key</c>).
    /// </summary>
    public string DefaultHeaderName { get; set; } = DefaultHeaderNameValue;

    /// <summary>
    /// Gets or sets the query parameter name to use when the scheme does not
    /// specify one (default: <c>api_key</c>).
    /// </summary>
    public string DefaultQueryName { get; set; } = DefaultQueryNameValue;

    /// <summary>
    /// Gets or sets the cookie name to use when the scheme does not specify
    /// one (default: <c>api_key</c>).
    /// </summary>
    public string DefaultCookieName { get; set; } = DefaultCookieNameValue;
}