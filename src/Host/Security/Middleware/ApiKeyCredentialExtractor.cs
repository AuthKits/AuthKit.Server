using System.Security;
using System.Security.Claims;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using Host.Security.LocationExtractors;
using Host.Security.Registry;
using Host.Security.Validation;
using Microsoft.AspNetCore.Http.Features;

namespace Host.Security.Middleware;

/// <summary>
/// Pipeline middleware that authenticates requests using the API key scheme
/// declared on the current request's endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The scheme is resolved per request from endpoint metadata
/// (<see cref="SecuritySchemeAttribute"/>) and the host's
/// <see cref="ISecuritySchemeRegistry"/> rather than from an ambient descriptor
/// registered in the container. Endpoints that do not declare a scheme are
/// passed through untouched.
/// </para>
/// <para>
/// Authentication is fail-closed: a request that carries a credential for a
/// declared scheme and cannot be authenticated is rejected with
/// <c>401 Unauthorized</c>. Only requests that carry no credential at all are
/// passed through so downstream middleware can decide how to handle them.
/// </para>
/// <para>
/// An <see cref="IApiKeyValidator"/> is resolved lazily from the request's
/// service provider, and only when a credential is actually present. A missing
/// validator registration is treated as a host configuration error and the
/// request fails closed with <c>401 Unauthorized</c>.
/// </para>
/// </remarks>
public sealed class ApiKeyCredentialExtractor(
    RequestDelegate next,
    ISecuritySchemeRegistry registry,
    IApiKeyLocationExtractorRegistry locationRegistry,
    ILogger<ApiKeyCredentialExtractor> logger)
{
    private const string AuthenticationType = "ApiKey";

    /// <summary>
    /// Extracts and validates the API key declared for the current request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var scheme = ResolveScheme(context);
        if (scheme is null)
        {
            await next(context);
            return;
        }

        string? apiKey;
        try
        {
            apiKey = await locationRegistry.Resolve(scheme.In).ExtractAsync(context, scheme);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Failed to extract API key from {Location} for scheme {Scheme}", scheme.In, scheme.Name);
            WriteUnauthorized(context);
            return;
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            await next(context);
            return;
        }

        var validator = context.RequestServices.GetService<IApiKeyValidator>();
        if (validator is null)
        {
            logger.LogError(
                "Scheme {Scheme} is declared on an endpoint, but no IApiKeyValidator is registered. " +
                "Register a validator during host or plugin configuration.",
                scheme.Name);
            WriteUnauthorized(context);
            return;
        }

        try
        {
            var principal = await validator.ValidateAsync(apiKey);
            if (principal is null)
            {
                logger.LogWarning("API key rejected by validator for scheme {Scheme}", scheme.Name);
                WriteUnauthorized(context);
                return;
            }

            var identity = new ClaimsIdentity(principal.Claims, AuthenticationType);
            context.User = new ClaimsPrincipal(identity);
            logger.LogDebug("API key validated for {Subject} via scheme {Scheme}", principal.Subject, scheme.Name);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Failed to validate API key for scheme {Scheme}", scheme.Name);
            WriteUnauthorized(context);
            return;
        }
        catch (SecurityException ex)
        {
            logger.LogWarning(ex, "Failed to validate API key for scheme {Scheme}", scheme.Name);
            WriteUnauthorized(context);
            return;
        }

        await next(context);
    }

    private AuthKitSecuritySchemeDescriptor? ResolveScheme(HttpContext context)
    {
        var schemeName = context.Features.Get<IEndpointFeature>()?.Endpoint
            ?.Metadata.GetMetadata<SecuritySchemeAttribute>()?.SchemeName;

        if (string.IsNullOrEmpty(schemeName))
            return null;

        if (!registry.TryGet(schemeName, out var scheme))
        {
            throw new InvalidOperationException(
                $"Endpoint declares security scheme '{schemeName}', but no enabled plugin contributes a scheme " +
                $"with that name. The request cannot be authenticated.");
        }

        return scheme;
    }

    private static void WriteUnauthorized(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "ApiKey";
    }
}