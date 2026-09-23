using Host.Plugins.Loading;
using Host.Plugins.Configuration;
using Host.Restful.Middleware.Exceptions;
using Host.Security.Middleware;
using AuthKit.Plugins.Abstractions.Pipeline;

namespace Host.Configuration.Pipeline;

/// <summary>
/// Provides extension methods for configuring the application's HTTP middleware
/// pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Configures routing, validation and exception handling, plugin provided
/// middleware, and authentication and authorization.
/// </para>
/// <para>
/// New pipeline hooks are inserted at their strongly typed
/// <see cref="PluginPipelinePosition"/>. Plugins at the same position are
/// ordered by stable plugin ID. Legacy <see cref="AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin.MiddlewareType"/>
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
        ConfigurePluginSlot(PluginPipelinePosition.BeforeRouting, PipelinePosition.BeforeRouting);
        app.UseRouting();
        ConfigurePluginSlot(PluginPipelinePosition.AfterRouting, PipelinePosition.AfterRouting);

        app.UseMiddleware<ValidationExceptionMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        PluginApplicationConfiguration.ConfigureLegacyMiddleware(app, plugins);
        ConfigurePluginSlot(PluginPipelinePosition.BeforeAuthentication, PipelinePosition.BeforeAuthentication);

        app.UseMiddleware<ApiKeyCredentialExtractor>();

        app.UseAuthentication();
        ConfigurePluginSlot(PluginPipelinePosition.AfterAuthentication);
        ConfigurePluginSlot(PluginPipelinePosition.BeforeAuthorization);
        app.UseAuthorization();
        ConfigurePluginSlot(PluginPipelinePosition.AfterAuthorization, PipelinePosition.AfterAuthorization);
        ConfigurePluginSlot(PluginPipelinePosition.BeforeEndpoints, PipelinePosition.BeforeEndpoints);

        // AfterEndpointExecution is post endpoint (response) execution: registered here, before
        // endpoint mapping, so each middleware wraps the endpoint and its post-next code runs
        // after the endpoint has executed.
        ConfigureMiddlewareSlot(PipelinePosition.AfterEndpointExecution);

        return app;

        void ConfigurePluginSlot(
            PluginPipelinePosition pipelinePosition,
            PipelinePosition? middlewarePosition = null)
        {
            PluginApplicationConfiguration.ConfigurePipeline(app, plugins, pipelinePosition);

            if (middlewarePosition is { } position)
                ConfigureMiddlewareSlot(position);
        }

        void ConfigureMiddlewareSlot(PipelinePosition middlewarePosition) =>
            PluginApplicationConfiguration.ConfigurePluginMiddlewares(app, plugins, middlewarePosition);
    }
}
