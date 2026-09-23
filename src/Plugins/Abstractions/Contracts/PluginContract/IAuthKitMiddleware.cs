using Microsoft.AspNetCore.Http;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

/// <summary>
/// DI-aware middleware contract. The host resolves the implementation from the
/// request service provider so scoped services share the single request scope.
/// </summary>
public interface IAuthKitMiddleware
{
    Task InvokeAsync(HttpContext context, RequestDelegate next);
}
