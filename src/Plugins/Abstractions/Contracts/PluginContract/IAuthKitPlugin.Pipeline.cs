using AuthKit.Plugins.Abstractions.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthKit.Plugins.Abstractions.Contracts.PluginContract;

public partial interface IAuthKitPlugin
{
    /// <summary>
    /// Registers plugin-owned endpoints during host endpoint configuration.
    /// </summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    /// <remarks>
    /// This optional hook runs after host services are configured and before
    /// the application starts processing requests. Exceptions are propagated.
    /// </remarks>
    void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <summary>
    /// Configures plugin application middleware on the actual host application.
    /// </summary>
    /// <param name="application">The application's a live builder.</param>
    void ConfigureApplication(IApplicationBuilder application)
    {
    }

    /// <summary>
    /// Gets the explicit pipeline position used by <see cref="ConfigurePipeline"/>.
    /// </summary>
    PluginPipelinePosition PipelinePosition => PluginPipelinePosition.BeforeAuthentication;

    /// <summary>
    /// Configures plugin middleware at the declared pipeline position.
    /// </summary>
    /// <param name="application">The application's a live builder.</param>
    /// <param name="position">The position currently being configured.</param>
    /// <remarks>
    /// The host invokes this hook once at <see cref="PipelinePosition"/>.
    /// Plugins at the same position are ordered by stable plugin ID.
    /// </remarks>
    void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position)
    {
    }
}