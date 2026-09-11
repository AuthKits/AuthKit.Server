using AuthKit.Plugins.Abstractions;

namespace Host.Security.Options;

/// <summary>
/// Options for configuring API key credential extraction.
/// </summary>
/// <remarks>
/// <para>
/// Defaults names are used by the location extractors whenever the declaring
/// scheme does not specify an explicit credential name.
/// </para>
/// <para>
/// <see cref="BufferThreshold"/> bounds how much of the request body is
/// buffered in memory before it spills to a temporary file.
/// </para>
/// </remarks>
public sealed class ApiKeyCredentialExtractorOptions
{
    /// <summary>
    /// Gets or sets the memory threshold, in bytes, used when buffering the
    /// request body for <see cref="AuthKitApiKeyLocation.Body"/> extraction.
    /// Bodies larger than this value spill to a temporary file (default:
    /// 1 MB).
    /// </summary>
    public long BufferThreshold { get; set; } = 1_048_576; // 1 MB

    /// <summary>
    /// Gets or sets the header name to use when the scheme does not specify
    /// one (default: <c>X-Api-Key</c>).
    /// </summary>
    public string DefaultHeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// Gets or sets the query parameter name to use when the scheme does not
    /// specify one (default: <c>api_key</c>).
    /// </summary>
    public string DefaultQueryName { get; set; } = "api_key";

    /// <summary>
    /// Gets or sets the cookie name to use when the scheme does not specify
    /// one (default: <c>api_key</c>).
    /// </summary>
    public string DefaultCookieName { get; set; } = "api_key";
}