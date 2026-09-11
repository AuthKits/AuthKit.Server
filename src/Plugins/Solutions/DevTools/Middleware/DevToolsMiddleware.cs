using System.Text;
using System.Text.Json;
using DevTools.Catalog;
using DevTools.Runtime;
using DevTools.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevTools.Middleware;

/// <summary>
/// Serves the developer tooling contributed by the plugin: Swagger UI for the
/// REST API and the gRPC UI for browsing and invoking gRPC methods.
/// </summary>
/// <remarks>
/// <para>
/// The middleware owns the routing for the tool pages and their JSON APIs.
/// It also serves a small landing page linking both tools.
/// </para>
/// <para>
/// All other paths are delegated to the remaining pipeline, so authentication,
/// REST endpoints, and gRPC endpoints continue to work unchanged.
/// </para>
/// </remarks>
public sealed class DevToolsMiddleware(RequestDelegate next, IOptions<DevToolsOptions> options,
    IGrpcServiceCatalog catalog, GrpcDynamicInvoker invoker, SwaggerHost swagger,
    IHostEnvironment environment, ILogger<DevToolsMiddleware> logger)
{
    private static readonly Lazy<string> GrpcUiPage = new(LoadEmbeddedPage);

    /// <summary>
    /// Routes the request to the gRPC UI, Swagger UI, or landing page, and
    /// otherwise delegates to the remaining pipeline.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var grpcPathBase = options.Value.GrpcUiPathBase.TrimEnd('/');

        if (PathMatches(context, grpcPathBase, out var grpcRelative))
        {
            await ServeGrpcUiAsync(context, grpcRelative);
            return;
        }

        var swaggerEnabled = options.Value.Swagger.Enabled ?? environment.IsDevelopment();
        if (swaggerEnabled && await swagger.TryServeAsync(context))
            return;

        if (PathMatches(context, options.Value.PathBase, out _))
        {
            await ServeLandingAsync(context);
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Determines whether the request path starts with the given prefix and
    /// returns the remainder of the path.
    /// </summary>
    private static bool PathMatches(HttpContext context, string pathBase, out string relative)
    {
        var path = context.Request.Path.Value ?? "/";
        relative = "";

        if (string.IsNullOrEmpty(pathBase))
            return false;

        if (!path.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = path[pathBase.Length..];
        if (rest.Length > 0 && !rest.StartsWith('/'))
            return false;

        relative = rest;
        return true;
    }

    /// <summary>
    /// Serves the gRPC UI page and its JSON API endpoints.
    /// </summary>
    private async Task ServeGrpcUiAsync(HttpContext context, string relative)
    {
        switch (relative)
        {
            case "":
                context.Response.Redirect(options.Value.GrpcUiPathBase.TrimEnd('/') + "/");
                return;
            case "/":
            case "/index.html":
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(RenderGrpcUiPage(), context.RequestAborted);
                return;
            }
            case "/api/services":
                await ApiServicesAsync(context);
                return;
            case "/api/invoke" when HttpMethods.IsPost(context.Request.Method):
                await ApiInvokeAsync(context);
                return;
            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
        }
    }

    /// <summary>
    /// Serves the landing page linking the Swagger UI and the gRPC UI.
    /// </summary>
    private async Task ServeLandingAsync(HttpContext context)
    {
        var grpcPathBase = options.Value.GrpcUiPathBase.TrimEnd('/');
        var swaggerPrefix = "/" + options.Value.Swagger.RoutePrefix.Trim('/');

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><title>AuthKit DevTools</title>
            <style>
              body { font: 15px/1.6 -apple-system,"Segoe UI",Roboto,sans-serif; background:#0f1419; color:#e6edf3;
                     display:grid; place-items:center; min-height:100vh; margin:0; }
              .card { background:#161c24; border:1px solid #2a3646; border-radius:10px; padding:28px 34px; text-align:center; }
              h1 { font-size:20px; margin:0 0 18px; }
              a { display:block; margin:8px 0; padding:12px 18px; border-radius:8px; text-decoration:none;
                   color:#fff; background:#4f9cf9; font-weight:600; }
              a:hover { filter:brightness(1.1); }
              .meta { color:#8b98a5; font-size:12px; margin-top:14px; }
            </style></head>
            <body><div class="card">
              <h1>AuthKit DevTools</h1>
              <a href="__SWAGGER__">Swagger UI</a>
              <a href="__GRPCUI__">gRPC UI</a>
              <div class="meta">dev environment · Swagger is only served in Development</div>
            </div></body></html>
            """.Replace("__SWAGGER__", swaggerPrefix)
              .Replace("__GRPCUI__", grpcPathBase + "/"),
            context.RequestAborted);
    }

    /// <summary>
    /// Serves the JSON payload listing the discovered gRPC services.
    /// </summary>
    private async Task ApiServicesAsync(HttpContext context)
    {
        var response = new GrpcCatalogResponse
        {
            Target = options.Value.ResolveGrpcTarget(),
            Services = catalog.GetServices()
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    /// <summary>
    /// Reads the invocation request and writes the invocation result as JSON.
    /// </summary>
    /// <remarks>
    /// Malformed JSON bodies are answered with <c>400 Bad Request</c>.
    /// </remarks>
    private async Task ApiInvokeAsync(HttpContext context)
    {
        var jsonOptions = context.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions
            ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

        try
        {
            var request = await JsonSerializer.DeserializeAsync<GrpcInvocationRequest>(
                context.Request.Body,
                jsonOptions,
                cancellationToken: context.RequestAborted);

            if (request is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Request body is required." }, jsonOptions, context.RequestAborted);
                return;
            }

            logger.LogInformation("gRPC UI invoking {Service}/{Method}.", request.Service, request.Method);

            var result = await invoker.InvokeAsync(request, context.RequestAborted);

            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(result, jsonOptions, context.RequestAborted);
        }
        catch (JsonException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message }, jsonOptions, context.RequestAborted);
        }
    }

    private static string LoadEmbeddedPage()
    {
        var assembly = typeof(DevToolsMiddleware).Assembly;
        const string resourceName = "DevTools.UI.ui.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private string RenderGrpcUiPage() =>
        GrpcUiPage.Value.Replace("{{pathBase}}", options.Value.GrpcUiPathBase.TrimEnd('/'));
}