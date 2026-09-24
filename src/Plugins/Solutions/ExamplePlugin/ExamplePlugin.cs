using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Contracts.SecuritySchemes;
using AuthKit.Plugins.Abstractions.Models;
using AuthKit.Plugins.Abstractions.Pipeline;
using ExamplePlugin.Authentication;
using ExamplePlugin.Grpc;
using ExamplePlugin.Hosting;
using ExamplePlugin.Middleware;
using ExamplePlugin.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace ExamplePlugin;

/// <summary>
/// End-to-end example plugin exercising the full IAuthKitPlugin contract.
/// </summary>
/// <remarks>
/// <para>
/// This plugin is a living reference: it implements every hook the contract exposes so
/// authors can copy the parts their plugin needs. It is intentionally small and has no
/// external dependencies beyond ASP.NET Core and the AuthKit abstractions.
/// </para>
/// <list type="bullet">
/// <item>Metadata through <see cref="PluginMetadataAttribute"/> (identity, capabilities, dependencies).</item>
/// <item>Configuration through <c>ConfigureServices(IServiceCollection, AuthKitPluginContext)</c>.</item>
/// <item>Middleware through <see cref="MiddlewareType"/> and <c>ConfigureApplication</c>.</item>
/// <item>Endpoints through <c>MapEndpoints</c>.</item>
/// <item>Security schemes, authentication, and authorization.</item>
/// <item>Structured health checks through <c>CheckHealthAsync</c>.</item>
/// <item>Lifecycle hooks and a plugin-owned hosted service.</item>
/// </list>
/// </remarks>
[PluginMetadata(
    id: "authkit.example",
    version: "1.0.0",
    tags: ["example", "reference", "template"],
    dependsOn: [],
    capabilities: ["example", "reference"],
    name: "ExamplePlugin",
    displayName: "Example Plugin",
    description: "Living reference implementing the full IAuthKitPlugin contract.",
    author: "AuthKit Contributors",
    license: "MIT",
    licenseUrl: "https://opensource.org/licenses/MIT",
    homepage: "https://example.org/example",
    repositoryUrl: "https://example.org/example.git",
    priority: 100,
    isEnabled: true,
    minHostVersion: "0.5.0"
)]
public sealed class ExamplePlugin : IAuthKitPlugin
{
    /// <summary>
    /// Registers the plugin's options, services, and hosted services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register plugin services.</param>
    /// <param name="context">Stable plugin context including the plugin-scoped configuration section.</param>
    /// <remarks>
    /// The plugin binds its options from <see cref="AuthKitPluginContext.Configuration"/>, which is
    /// scoped to <c>Plugins:authkit.example</c> (or the plugin name when the ID section is absent).
    /// The same section is available to any plugin services through the options infrastructure.
    /// </remarks>
    public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
    {
        services.Configure<ExampleOptions>(context.Configuration);

        services.AddSingleton(TimeProvider.System);
        // Registered so the host can resolve ExampleScopedMiddleware (IAuthKitMiddleware)
        // from the request service provider within the single request scope.
        services.AddScoped<ExampleScopedMiddleware>();
    }

    /// <summary>
    /// The legacy middleware entry point. The host inserts this type at the plugin
    /// middleware slot when the plugin does not implement <c>ConfigureApplication</c>
    /// or <c>ConfigurePipeline</c>.
    /// </summary>
    /// <remarks>
    /// See <c>ExampleProtocolMiddleware</c> for the conventional middleware contract.
    /// </remarks>
    public Type MiddlewareType => typeof(ExampleProtocolMiddleware);

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
    /// Registers the reference endpoints on the application's route builder.
    /// </summary>
    /// <param name="endpoints">The application's endpoint route builder.</param>
    /// <remarks>
    /// Endpoints protected by <see cref="SecuritySchemeAttribute"/> participate in the host's
    /// request-aware security resolution. The scheme name must match a key returned by
    /// <see cref="GetSecuritySchemes"/>.
    /// </remarks>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/example/hello", ([FromServices] IOptions<ExampleOptions> options) =>
                Results.Ok(new { options.Value.Greeting }))
            .WithMetadata(new SecuritySchemeAttribute("example-api-key"));

        endpoints.MapGrpcService<ExampleGreeterService>();
    }

    /// <summary>
    /// Configures the plugin application middleware on the actual host application.
    /// </summary>
    /// <param name="application">The application's pipeline builder.</param>
    /// <remarks>
    /// When implemented, this hook takes precedence over <see cref="MiddlewareType"/>.
    /// </remarks>
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

    /// <summary>
    /// Performs a structured health check of the plugin's dependencies.
    /// </summary>
    /// <param name="services">The root service provider of the host application.</param>
    /// <param name="cancellationToken">A token that can cancel the health check.</param>
    /// <returns>Structured health results for each checked capability.</returns>
    /// <remarks>
    /// The reference implementation always reports healthy to keep the example self-contained;
    /// real plugins resolve their dependencies from <paramref name="services"/> and report
    /// degraded or unhealthy states with matching reasons, data, and tags.
    /// </remarks>
    public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;

        return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
        [
            new(
                PluginHealthStatus.Healthy,
                "ExamplePlugin is operational.",
                new Dictionary<string, object>
                {
                    ["capability"] = "example",
                    ["utcNow"] = timeProvider.GetUtcNow().ToString("O")
                },
                ["example", "readiness"])
        ]);
    }

    /// <summary>
    /// Contributes the plugin's OpenAPI security scheme metadata.
    /// </summary>
    /// <returns>
    /// A readonly dictionary keyed by security scheme name. Keys must match the
    /// descriptor's <see cref="AuthKitSecuritySchemeDescriptor.Name"/>.
    /// </returns>
    public IReadOnlyDictionary<string, AuthKitSecuritySchemeDescriptor> GetSecuritySchemes() =>
        new Dictionary<string, AuthKitSecuritySchemeDescriptor>
        {
            ["example-api-key"] = new()
            {
                Name = "example-api-key",
                Type = AuthKitSecuritySchemeType.ApiKey,
                In = AuthKitApiKeyLocation.Header,
                CredentialName = "X-Example-Api-Key",
                Description = "Reference API key scheme contributed by ExamplePlugin."
            }
        };

    /// <summary>
    /// Registers the reference API key authentication scheme on the host builder.
    /// </summary>
    /// <param name="builder">The host authentication builder.</param>
    /// <remarks>
    /// The scheme is registered without changing the host default scheme. See
    /// <see cref="ExampleApiKeyAuthenticationHandler"/> for the minimal handler.
    /// </remarks>
    public void ConfigureAuthentication(AuthenticationBuilder builder)
    {
        builder.AddScheme<AuthenticationSchemeOptions, ExampleApiKeyAuthenticationHandler>(
            ExampleApiKeyAuthenticationHandler.SchemeName,
            _ => { });
    }

    /// <summary>
    /// Registers a reference authorization policy used by plugin endpoints.
    /// </summary>
    /// <param name="options">The host authorization options.</param>
    /// <remarks>
    /// Policy names are globally significant; use namespaced names to avoid collisions
    /// with the host or other plugins.
    /// </remarks>
    public void ConfigureAuthorization(AuthorizationOptions options)
    {
        options.AddPolicy(
            "example.read",
            policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(ExampleApiKeyAuthenticationHandler.SchemeName));
    }

    /// <summary>
    /// Initializes plugin runtime resources before the host is considered started.
    /// </summary>
    public Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Notifies the plugin after the host has started successfully.
    /// </summary>
    public Task OnStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Releases plugin runtime resources during a graceful host shutdown.
    /// </summary>
    public Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Returns the plugin-owned hosted services registered in the host DI container.
    /// </summary>
    /// <remarks>
    /// The host registers each service returned here as a singleton <see cref="IHostedService"/>
    /// and starts and stops it with the application.
    /// </remarks>
    public IReadOnlyList<IHostedService> GetHostedServices() => [new ExampleBackgroundService(
        new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExampleBackgroundService>(),
        TimeProvider.System)];
}