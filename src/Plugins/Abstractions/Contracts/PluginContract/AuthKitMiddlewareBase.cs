using Microsoft.AspNetCore.Http;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

/// <summary>
/// Convention based convenience base class for plugin middleware.
/// Plugins implement <see cref="InvokeAsync"/> directly without needing DI.
/// </summary>
public abstract class AuthKitMiddlewareBase
{
    public abstract Task InvokeAsync(HttpContext context, RequestDelegate next);
}
