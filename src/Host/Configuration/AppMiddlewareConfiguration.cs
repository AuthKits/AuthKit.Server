using Host.Plugins;
using Host.Restful.Middleware.Exceptions;
using Host.Security.Middleware;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;

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
/// New pipeline hooks are inserted at their strongly typed
/// <see cref="PluginPipelinePosition"/>. Plugins at the same position are
/// ordered by stable plugin ID. Legacy <see cref="IAuthKitPlugin.MiddlewareType"/>
/// middleware remains in its original slot unless the plugin opts into a new
/// application or pipeline hook.
/// </para>
/// </remarks>
public static class AppMiddlewareConfiguration
{
    /// <summary>
    /// Configures the applications HTTP middleware pipeline.
    /// </summary>
    /// <param name="plugins">
    /// The plugins loaded during application startup. Plugins may optionally
    /// contribute middleware, application hooks, or positioned pipeline hooks.
    /// </param>
    /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
    public static WebApplication ConfigureMiddleware(
        this WebApplication app,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        PluginApplicationConfiguration.ConfigureApplications(app, plugins);
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.BeforeRouting);
        app.UseRouting();
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.AfterRouting);

        app.UseMiddleware<ValidationExceptionMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        PluginApplicationConfiguration.ConfigureLegacyMiddleware(app, plugins);
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.BeforeAuthentication);

        app.UseMiddleware<ApiKeyCredentialExtractor>();

        app.UseAuthentication();
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.AfterAuthentication);
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.BeforeAuthorization);
        app.UseAuthorization();
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.AfterAuthorization);
        PluginApplicationConfiguration.ConfigurePipeline(app, plugins, PluginPipelinePosition.BeforeEndpoints);

        return app;
    }
}
