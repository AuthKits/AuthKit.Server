using Host.Grpc;

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
    /// Registers gRPC services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection used to register gRPC services.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            //options.Interceptors.Add<ExceptionHandlingInterceptor>();
        });

        return services;
    }

    /// <summary>
    /// Maps gRPC service endpoints to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>The configured <see cref="WebApplication"/> for method chaining.</returns>
    public static WebApplication MapGrpcEndpoints(this WebApplication app)
    {
        app.MapGrpcService<GreeterService>();
        return app;
    }
}
