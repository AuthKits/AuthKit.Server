using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Microsoft.Extensions.Options;

using Host.Security.Options;

namespace Host.Security.LocationExtractors;

/// <summary>
/// Base class for <see cref="IApiKeyLocationExtractor"/> implementations that
/// resolve credential names using configured defaults.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes declare the credential location they handle through
/// <see cref="Location"/> and implement credential extraction in
/// <see cref="ExtractAsync"/>.
/// </para>
/// <para>
/// <see cref="ResolveName"/> uses the configured default when the security
/// scheme does not provide an explicit credential name.
/// </para>
/// </remarks>
public abstract class ApiKeyLocationExtractorBase : IApiKeyLocationExtractor
{
    /// <summary>
    /// Gets the options controlling credential extraction defaults and
    /// request body buffering behavior.
    /// </summary>
    protected ApiKeyCredentialExtractorOptions Options { get; }

    /// <summary>
    /// Initializes new instance of the
    /// <see cref="ApiKeyLocationExtractorBase"/> class.
    /// </summary>
    /// <param name="options">The options controlling credential extraction.</param>
    protected ApiKeyLocationExtractorBase(
        IOptions<ApiKeyCredentialExtractorOptions> options) =>
        Options = options.Value;

    /// <summary>
    /// Gets the API key location handled by this extractor.
    /// </summary>
    public abstract AuthKitApiKeyLocation Location { get; }

    /// <summary>
    /// Extracts an API key from the specified HTTP request.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="scheme">The security scheme describing the credential.</param>
    /// <returns>The extracted API key, or null if the credential is not present.</returns>
    public abstract Task<string?> ExtractAsync(HttpContext context, AuthKitSecuritySchemeDescriptor scheme);

    /// <summary>
    /// Resolves the credential name using the configured or provided default.
    /// </summary>
    /// <param name="credentialName">The credential field name declared by the scheme.</param>
    /// <param name="defaultValue">The default name used when the scheme is empty.</param>
    /// <returns>The resolved credential name.</returns>
    protected static string ResolveName(string? credentialName, string defaultValue) =>
        string.IsNullOrWhiteSpace(credentialName)
            ? defaultValue
            : credentialName;
}