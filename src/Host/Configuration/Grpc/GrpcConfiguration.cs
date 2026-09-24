using Host.Grpc;
using Host.Plugins.Configuration;
using Host.Plugins.Loading;
using Microsoft.Extensions.Logging;

namespace Host.Configuration.Grpc;

/// <summary>
/// Provides extension methods for configuring gRPC services and endpoints.
/// </summary>
/// <remarks>
/// <para>
/// Registers gRPC services with the applications dependency injection
/// container and maps the available gRPC service implementations to the
/// request pipeline.
/// Global gRPC interceptors can be registered through the gRPC configuration
/// when cross cutting concerns such as exception handling or request logging
/// are required.
/// </para>
/// </remarks>
public static class GrpcConfiguration
{
    /// <summary>
    /// Registers gRPC services with the dependency injection container,
    /// including plugin contributed interceptors.
    /// </summary>
    /// <param name="services">The service collection used to register gRPC services.</param>
    /// <param name="plugins">The plugins loaded during application startup.</param>
    /// <param name="logger">Logger for gRPC composition diagnostics.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddGrpcServices(
        this IServiceCollection services,
        IReadOnlyList<LoadedPlugin> plugins,
        ILogger logger)
    {
        services.AddGrpc(options =>
        {
            //options.Interceptors.Add<ExceptionHandlingInterceptor>();
        });

        services.AddPluginGrpcInterceptors(plugins, logger);

        return services;
    }

    /// <summary>
    /// Maps gRPC service endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>The configured <see cref="WebApplication"/> for method chaining.</returns>
    public static WebApplication MapGrpcEndpoints(this WebApplication app)
    {
        app.MapGrpcService<JwksService>();
        app.MapGrpcService<MonitoringService>();
        return app;
    }
}
