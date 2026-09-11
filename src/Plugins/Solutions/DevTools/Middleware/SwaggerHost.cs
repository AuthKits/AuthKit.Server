using System.Reflection;
using DevTools.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace DevTools.Middleware;

/// <summary>
/// Serves the OpenAPI document and the Swagger UI contributed by the host's
/// Swagger generation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The host generates the OpenAPI document (SwaggerGen) but no longer
/// wires the Swashbuckle serving middleware.
/// </para>
/// <para>
/// Both serving middlewares are internal to Swashbuckle, so they are
/// instantiated and invoked through reflection. The middleware instances
/// are constructed once and reused for every request.
/// </para>
/// </remarks>
public sealed class SwaggerHost(
    IOptions<DevToolsOptions> options,
    IConfiguration configuration,
    ISwaggerProvider swaggerProvider,
    ILogger<SwaggerHost> logger)
{
    private static readonly Type SwaggerMiddlewareType = typeof(SwaggerOptions).Assembly
        .GetType("Swashbuckle.AspNetCore.Swagger.SwaggerMiddleware", throwOnError: true)!;

    private static readonly Type SwaggerUiMiddlewareType = typeof(SwaggerUIOptions).Assembly
        .GetType("Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIMiddleware", throwOnError: true)!;

    private static readonly MethodInfo SwaggerInvoke = SwaggerMiddlewareType
        .GetMethod("Invoke", [typeof(HttpContext), typeof(ISwaggerProvider)])!;

    private static readonly MethodInfo SwaggerUiInvoke = SwaggerUiMiddlewareType
        .GetMethod("Invoke", [typeof(HttpContext)])!;

    private readonly (string RoutePrefix, object Document, object Ui) _content =
        CreateContent(options, configuration, logger);

    /// <summary>
    /// Returns whether the request path belongs to the Swagger document or UI
    /// and, when it does, serves it.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <returns>
    /// <c>true</c> when the request was handled by the Swagger document or UI;
    /// otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> TryServeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (!path.StartsWith(_content.RoutePrefix + "/", StringComparison.OrdinalIgnoreCase)
            && !path.Equals(_content.RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await InvokeAsync(SwaggerInvoke, _content.Document, context, swaggerProvider);

        if (context.Response.HasStarted)
            return true;

        await InvokeAsync(SwaggerUiInvoke, _content.Ui, context);
        return true;
    }

    /// <summary>
    /// Invokes the middleware method reflectively.
    /// </summary>
    private static async Task InvokeAsync(MethodInfo method, object instance, params object[] arguments)
    {
        var invocation = method.Invoke(instance, arguments) as Task
            ?? throw new InvalidOperationException(
                $"Middleware method '{method.Name}' did not return a task.");

        await invocation;
    }

    /// <summary>
    /// Builds the reflection-based Swagger document and UI middleware instances
    /// together with the resolved route prefix.
    /// </summary>
    /// <remarks>
    /// Each instance is constructed once and reused for every request.
    /// </remarks>
    private static (string RoutePrefix, object Document, object Ui) CreateContent(
        IOptions<DevToolsOptions> options,
        IConfiguration configuration,
        ILogger<SwaggerHost> logger)
    {
        var swagger = options.Value.Swagger;
        var routePrefix = "/" + swagger.RoutePrefix.Trim('/');

        var specVersion = ResolveSpecVersion(swagger.SpecVersion, configuration, logger);

        var document = Activator.CreateInstance(
            SwaggerMiddlewareType,
            (RequestDelegate)PassThrough,
            new SwaggerOptions
            {
                RouteTemplate = $"{routePrefix}/{{documentName}}/swagger.json",
                OpenApiVersion = specVersion
            })!;

        var uiOptions = new SwaggerUIOptions
        {
            RoutePrefix = swagger.RoutePrefix.Trim('/'),
            DocumentTitle = swagger.DocumentTitle,
            IndexStream = () => typeof(SwaggerUIOptions).Assembly
                .GetManifestResourceStream("Swashbuckle.AspNetCore.SwaggerUI.index.html")
        };

        uiOptions.SwaggerEndpoint(
            $"/{swagger.RoutePrefix.Trim('/')}/{swagger.DocumentName}/swagger.json",
            swagger.DocumentName);

        var ui = Activator.CreateInstance(SwaggerUiMiddlewareType, (RequestDelegate)NotFound, uiOptions)!;

        logger.LogInformation("Swagger UI configured at {RoutePrefix} (spec {SpecVersion}).",
            routePrefix, specVersion.ToString());

        return (routePrefix, document, ui);

        Task PassThrough(HttpContext _) => Task.CompletedTask;

        Task NotFound(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Resolves the OpenAPI spec version from the options override, the host
    /// configuration, or a sensible default.
    /// </summary>
    private static OpenApiSpecVersion ResolveSpecVersion(
        string? overrideValue,
        IConfiguration configuration,
        ILogger logger)
    {
        var value = overrideValue ?? configuration["OpenApi:SpecVersion"]?.Trim() ?? "3.0";

        if (value.Equals("3.0", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("3.0.", StringComparison.OrdinalIgnoreCase))
            return OpenApiSpecVersion.OpenApi3_0;

        if (value.Equals("3.1", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("3.1.", StringComparison.OrdinalIgnoreCase))
            return OpenApiSpecVersion.OpenApi3_1;

        logger.LogWarning("Unknown OpenApi:SpecVersion '{Value}'; falling back to 3.0.", value);
        return OpenApiSpecVersion.OpenApi3_0;
    }
}