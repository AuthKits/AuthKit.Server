using System.Security.Claims;
using AuthKit.Plugins.Abstractions;
using Host.Security.LocationExtractors;
using Host.Security.Validation;

namespace Host.Security.Middleware;

/// <summary>
/// Pipeline middleware that authenticates requests using an API key described by the
/// request's <see cref="AuthKitSecuritySchemeDescriptor"/>.
/// </summary>
/// <remarks>
/// <para>
/// The middleware resolves the correct credential location strategy from
/// <see cref="IApiKeyLocationExtractorRegistry"/>, extracts the API key, and validates
/// it through <see cref="IApiKeyValidator"/>. On success the principal's claims are
/// attached to <see cref="HttpContext.User"/> as an authenticated identity.
/// </para>
/// <para>
/// When no scheme is declared for the request, or when extraction or validation does
/// not produce a principal, the pipeline continues to the next middleware so downstream
/// authentication can decide how to handle the request.
/// </para>
/// </remarks>
public sealed class ApiKeyCredentialExtractor(
    RequestDelegate next,
    IApiKeyLocationExtractorRegistry registry,
    ILogger<ApiKeyCredentialExtractor> logger)
{
    private const string AuthenticationType = "ApiKey";

    /// <summary>
    /// Extracts and validates the API key declared for the current request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="validator">The validator used to authenticate the extracted API key.</param>
    public async Task InvokeAsync(HttpContext context, IApiKeyValidator validator)
    {
        var scheme = context.RequestServices.GetService<AuthKitSecuritySchemeDescriptor>();
        if (scheme is null)
        {
            await next(context);
            return;
        }

        string? apiKey = null;

        try
        {
            apiKey = await registry.Resolve(scheme.In).ExtractAsync(context, scheme);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to extract API key from {Location} for scheme {Scheme}", scheme.In, scheme.Name);
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            try
            {
                var principal = await validator.ValidateAsync(apiKey);
                if (principal is not null)
                {
                    context.User.AddIdentity(new ClaimsIdentity(principal.Claims, AuthenticationType));
                    logger.LogDebug("API key validated for {Subject} via scheme {Scheme}", principal.Subject, scheme.Name);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to validate API key extracted from {Location} for scheme {Scheme}", scheme.In, scheme.Name);
            }
        }

        await next(context);
    }
}