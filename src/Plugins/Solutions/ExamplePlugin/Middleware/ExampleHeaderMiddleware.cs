using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using Microsoft.AspNetCore.Http;

namespace ExamplePlugin.Middleware;

/// <summary>
/// Reference convention based middleware. Implements <see cref="AuthKitMiddlewareBase.InvokeAsync"/>
/// directly without needing DI.
/// </summary>
public sealed class ExampleHeaderMiddleware : AuthKitMiddlewareBase
{
    public override async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Items["example.header.middleware"] = true;
        context.Response.Headers.Append("X-Example-Middleware", "header");
        await next(context);
    }
}
