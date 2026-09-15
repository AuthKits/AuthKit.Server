using AuthKit.Plugins.Abstractions.Models;
using Host.Monitoring;
using Host.Plugins.Loading;
using Host.Plugins.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Host.Configuration.Pipeline;

/// <summary>
/// Provides extension methods for mapping AuthKit application endpoints.
/// </summary>
/// <remarks>
/// <para>
/// Configures the root service information endpoint, controller routes,
/// health monitoring, and basic process uptime metrics.
/// </para>
/// <para>
/// The health endpoint verifies the availability of the JWT key store and
/// executes health checks contributed by loaded authentication plugins.
/// </para>
/// </remarks>
public static class EndpointConfiguration
{
    /// <summary>
    /// Maps AuthKit application endpoints to the specified web application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="plugins">
    /// The plugins whose endpoint hooks are invoked after host endpoint services
    /// are configured and before request processing starts.
    /// </param>
    /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
    public static WebApplication MapAppEndpoints(
        this WebApplication app,
        IReadOnlyList<LoadedPlugin> plugins)
    {
        app.MapGet("/", () => Results.Json(new
        {
            name = "Auth Microservice",
            description = "Core service for authentication, authorization and identity management.",
            endpoints = new
            {
                rest = new
                {
                    baseUrl = "https://localhost:5000/api",
                    swagger = "https://localhost:5000/swagger"
                },
                grpc = new
                {
                    baseUrl = "https://localhost:5001",
                    note = "Use gRPC client to interact with this service."
                }
            },
            version = "1.0.0",
            environment = app.Environment.EnvironmentName
        }));
        app.MapControllers();
        PluginApplicationConfiguration.MapEndpoints(app, plugins);

        app.MapGet("/health", async (
            [FromServices] HealthReportService healthReportService) =>
        {
            var report = await healthReportService.BuildAsync();
            var healthy = report.Status == nameof(PluginHealthStatus.Healthy);

            return Results.Json(
                report,
                statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
        })
            .WithName("HealthCheck")
            .WithTags("Monitoring");

        app.MapGet("/metrics", () => Results.Json(new
        {
            uptime = MetricsReportService.Build().UptimeSeconds
        }))
            .WithName("Metrics")
            .WithTags("Monitoring");

        return app;
    }
}
