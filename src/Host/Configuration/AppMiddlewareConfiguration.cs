using Host.Plugins;
using Host.Restful.Middleware.Exceptions;

namespace Host.Configuration;

/// <summary>
/// Provides extension methods for configuring the application's HTTP middleware
/// pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Configures routing, validation and exception handling, plugin-provided
/// middleware, and authentication and authorization.
/// </para>
/// <para>
/// Plugin middleware is inserted after the host exception handling middleware
/// and before authentication so plugins can participate in request processing
/// before the authenticated endpoint pipeline is reached.
/// </para>
/// </remarks>
public static class AppMiddlewareConfiguration
{
    /// <summary>
    /// Configures the applications HTTP middleware pipeline.
    /// </summary>
    /// <param name="plugins">
    /// The plugins loaded during application startup. Plugins may optionally
    /// contribute middleware through their configured middleware type.
    /// </param>
    /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
    public static WebApplication ConfigureMiddleware(
        this WebApplication app,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        app.UseRouting();

        app.UseMiddleware<ValidationExceptionMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        foreach (var plugin in plugins)
        {
            if (plugin.Plugin.MiddlewareType is { } middlewareType)
                app.UseMiddleware(middlewareType);
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
