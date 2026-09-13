using System.Diagnostics;
using AuthKit.Plugins.Abstractions.Models;
using Core.KeyManagement.Interfaces;
using Host.Plugins;

namespace Host.Configuration;

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

        app.MapGet("/health", async (HttpContext context, IJwtKeyStore keyStore, IReadOnlyList<LoadedPlugin> plugins) =>
        {
            var keyStoreHealthy = keyStore.GetPublicJwks().Any();

            var pluginResults = new Dictionary<string, IReadOnlyList<PluginHealthResult>>();
            foreach (var lp in plugins)
                pluginResults[lp.Plugin.Name] = await lp.Plugin.CheckHealthAsync(
                    context.RequestServices,
                    context.RequestAborted);

            var pluginStatus = pluginResults.Values
                .SelectMany(results => results)
                .Select(result => result.Status)
                .DefaultIfEmpty(PluginHealthStatus.Healthy)
                .Max();
            var status = keyStoreHealthy
                ? pluginStatus
                : PluginHealthStatus.Unhealthy;
            var healthy = status == PluginHealthStatus.Healthy;

            return Results.Json(new
            {
                status = status.ToString(),
                time = DateTime.UtcNow,
                jwtKeyStore = keyStoreHealthy ? "Healthy" : "Unhealthy",
                plugins = pluginResults
            }, statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
        })
            .WithName("HealthCheck")
            .WithTags("Monitoring");

        app.MapGet("/metrics", () => Results.Json(new { uptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime).TotalSeconds }))
            .WithName("Metrics")
            .WithTags("Monitoring");

        return app;
    }
}
