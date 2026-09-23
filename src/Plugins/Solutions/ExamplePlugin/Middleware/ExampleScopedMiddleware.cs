using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExamplePlugin.Middleware;

/// <summary>
/// Reference DI aware middleware. The host resolves it from the request service
/// provider so scoped services share the single request scope.
/// </summary>
public sealed class ExampleScopedMiddleware(ILogger<ExampleScopedMiddleware> logger, TimeProvider timeProvider) : IAuthKitMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        logger.LogDebug("ExampleScopedMiddleware handling request at {Now}.", timeProvider.GetUtcNow());
        context.Items["example.scoped.middleware"] = timeProvider.GetUtcNow().ToString("O");
        await next(context);
        // Post-endpoint response processing goes here (after next).
    }
}
