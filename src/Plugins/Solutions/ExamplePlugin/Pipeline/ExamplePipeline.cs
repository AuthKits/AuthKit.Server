using AuthKit.Plugins.Abstractions.Pipeline;
using ExamplePlugin.Grpc;
using ExamplePlugin.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

public sealed partial class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Declarative middleware registrations (issue #19, C1–C5). The plugin declares
    /// WHAT middleware it needs; the host owns activation, deterministic ordering
    /// (Order → stable PluginId → DeclarationIndex), and pipeline insertion.
    /// </summary>
    public IReadOnlyList<PluginMiddleware> Middlewares =>
    [
        // Convention-based middleware, ordered first at its position.
        new PluginMiddleware(
            typeof(ExampleHeaderMiddleware),
            AuthKit.Plugins.Abstractions.Pipeline.PipelinePosition.BeforeAuthentication,
            Order: 0,
            IsMiddlewareEnabled: true,
            Name: "example-header"),
        // DI-aware middleware (scoped services from the request scope).
        new PluginMiddleware(
            typeof(ExampleScopedMiddleware),
            AuthKit.Plugins.Abstractions.Pipeline.PipelinePosition.AfterAuthorization,
            Order: 10,
            IsMiddlewareEnabled: true,
            Name: "example-scoped"),
        // Disabled entry: host skips it without side effects or ordering impact.
        new PluginMiddleware(
            typeof(ExampleHeaderMiddleware),
            AuthKit.Plugins.Abstractions.Pipeline.PipelinePosition.BeforeEndpoints,
            Order: 0,
            IsMiddlewareEnabled: false,
            Name: "example-disabled"),
        // gRPC interceptor: composed into the host interceptor chain.
        new PluginMiddleware(
            typeof(ExampleLoggingInterceptor),
            AuthKit.Plugins.Abstractions.Pipeline.PipelinePosition.BeforeEndpoints,
            Order: 0,
            IsMiddlewareEnabled: true,
            Name: "example-grpc-logging",
            Transport: AuthKitTransport.Grpc),
    ];

    /// <summary>
    /// Configures the plugin application middleware on the actual host application.
    /// </summary>
    /// <param name="application">The application's pipeline builder.</param>
    public void ConfigureApplication(IApplicationBuilder application)
    {
        application.Use(async (HttpContext context, RequestDelegate next) =>
        {
            context.Response.Headers.Append("X-Example-Plugin", "1.0.0");
            await next(context);
        });
    }

    /// <summary>
    /// Demonstrates the plugin pipeline positioning hook.
    /// </summary>
    public PluginPipelinePosition PipelinePosition => PluginPipelinePosition.BeforeAuthentication;

    /// <summary>
    /// Registers a custom pipeline hook at the selected stage.
    /// </summary>
    public void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position)
    {
        if (position != PluginPipelinePosition.BeforeAuthentication)
            return;

        application.Use(async (context, next) =>
        {
            context.Items["example.pipeline.position"] = position;
            await next(context);
        });
    }
}
