using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExamplePlugin.Middleware;

/// <summary>
/// Reference middleware following the conventional ASP.NET Core middleware pattern.
/// </summary>
/// <remarks>
/// The constructor accepts <see cref="RequestDelegate"/> and the public
/// <c>InvokeAsync</c> method takes <see cref="HttpContext"/> and returns
/// <see cref="Task"/>. Additional dependencies are supplied through DI.
/// </remarks>
public sealed class ExampleProtocolMiddleware(RequestDelegate next, ILogger<ExampleProtocolMiddleware> logger)
{
    /// <summary>
    /// Adds a protocol header to every response and forwards to the next delegate.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Example-Plugin"] = "1.0.0";
            return Task.CompletedTask;
        });

        logger.LogDebug("ExamplePlugin middleware handling {Method} {Path}.", context.Request.Method, context.Request.Path);
        await next(context);
    }
}