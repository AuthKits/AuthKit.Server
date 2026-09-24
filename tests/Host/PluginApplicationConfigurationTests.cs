using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Pipeline;
using Host.Plugins.Configuration;
using Host.Plugins.Loading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin endpoint and application pipeline integration.
/// </summary>
public sealed class PluginApplicationConfigurationTests
{
    [Fact]
    public void MapEndpoints_UsesTheApplicationEndpointRouteBuilder()
    {
        var app = WebApplication.CreateBuilder().Build();
        var plugin = new EndpointPlugin();
        var loaded = Load(plugin);

        PluginApplicationConfiguration.MapEndpoints(app, [loaded]);

        var dataSources = ((IEndpointRouteBuilder)app).DataSources;

        Assert.Contains(dataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("PluginEndpoint", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ConfigureApplication_ReceivesTheActualApplicationBuilder()
    {
        var app = WebApplication.CreateBuilder().Build();
        var plugin = new ApplicationPlugin();

        PluginApplicationConfiguration.ConfigureApplications(app, [Load(plugin)]);

        Assert.Same(app, plugin.Application);
        Assert.Equal(1, plugin.Calls);
    }

    [Fact]
    public void PipelineOrdering_IsStableByPluginId()
    {
        var app = WebApplication.CreateBuilder().Build();
        var calls = new List<string>();
        var first = new PipelinePlugin("plugin.b", calls);
        var second = new PipelinePlugin("plugin.a", calls);

        PluginApplicationConfiguration.ConfigurePipeline(
            app,
            [Load(first), Load(second)],
            PluginPipelinePosition.AfterAuthentication);

        Assert.Equal(["plugin.a", "plugin.b"], calls);
    }

    [Fact]
    public async Task PipelineOrdering_IsVisibleInTheExecutedRequestPipeline()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var application = new ApplicationBuilder(services);
        var markers = new List<string>();
        var first = new MiddlewarePlugin("plugin.b", markers);
        var second = new MiddlewarePlugin("plugin.a", markers);

        PluginApplicationConfiguration.ConfigurePipeline(
            application,
            [Load(first), Load(second)],
            PluginPipelinePosition.AfterAuthentication);
        application.Run(context =>
        {
            markers.Add("endpoint");
            return Task.CompletedTask;
        });

        var pipeline = application.Build();
        await pipeline(new DefaultHttpContext());

        Assert.Equal(["plugin.a", "plugin.b", "endpoint"], markers);
    }

    [Fact]
    public void InvalidPipelinePosition_IsRejected()
    {
        var app = WebApplication.CreateBuilder().Build();
        var plugin = new InvalidPositionPlugin();

        Assert.Throws<InvalidOperationException>(() =>
            PluginApplicationConfiguration.ConfigurePipeline(
                app, [Load(plugin)], PluginPipelinePosition.BeforeRouting));
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadata("endpoint-plugin", "1.0.0", [], [], [], description: "Endpoint test")]
    private sealed class EndpointPlugin : IAuthKitPlugin
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
            endpoints.MapGet("/plugin-endpoint", () => Results.Ok()).WithDisplayName("PluginEndpoint");
    }

    [PluginMetadata("application-plugin", "1.0.0", [], [], [], description: "Application test")]
    private sealed class ApplicationPlugin : IAuthKitPlugin
    {
        public IApplicationBuilder? Application { get; private set; }
        public int Calls { get; private set; }

        public void ConfigureApplication(IApplicationBuilder application)
        {
            Calls++;
            Application = application;
        }
    }

    [PluginMetadata("pipeline-plugin", "1.0.0", [], [], [], description: "Pipeline test")]
    private sealed class PipelinePlugin : IAuthKitPlugin
    {
        private readonly string _id;
        private readonly List<string> _calls;

        public PipelinePlugin(string id, List<string> calls)
        {
            _id = id;
            _calls = calls;
        }

        public string Id => _id;
        public PluginPipelinePosition PipelinePosition => PluginPipelinePosition.AfterAuthentication;

        public void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position) =>
            _calls.Add(_id);
    }

    [PluginMetadata("middleware-plugin", "1.0.0", [], [], [], description: "Middleware ordering test")]
    private sealed class MiddlewarePlugin : IAuthKitPlugin
    {
        private readonly string _id;
        private readonly List<string> _markers;

        public MiddlewarePlugin(string id, List<string> markers)
        {
            _id = id;
            _markers = markers;
        }

        public string Id => _id;
        public PluginPipelinePosition PipelinePosition => PluginPipelinePosition.AfterAuthentication;

        public void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position)
        {
            application.Use(async (_, next) =>
            {
                _markers.Add(_id);
                await next();
            });
        }
    }

    [PluginMetadata("invalid-position-plugin", "1.0.0", [], [], [], description: "Invalid position test")]
    private sealed class InvalidPositionPlugin : IAuthKitPlugin
    {
        public PluginPipelinePosition PipelinePosition => (PluginPipelinePosition)999;

        public void ConfigurePipeline(IApplicationBuilder application, PluginPipelinePosition position)
        {
        }
    }

    [Fact]
    public void ConfigurePluginMiddlewares_SkipsGrpcTransportEntries()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        // Must not throw at Build(): a gRPC Interceptor has no Invoke/InvokeAsync
        // and would fail UseMiddleware activation if picked up by the HTTP pipeline.
        PluginApplicationConfiguration.ConfigurePluginMiddlewares(
            app, [Load(new GrpcOnlyPlugin())], PipelinePosition.BeforeEndpoints);

        var pipeline = app.Build();
        Assert.NotNull(pipeline);
    }

    [PluginMetadata("grpc-only-plugin", "1.0.0", [], [], [], description: "gRPC-only middleware test")]
    private sealed class GrpcOnlyPlugin : IAuthKitPlugin
    {
        public IReadOnlyList<PluginMiddleware> Middlewares =>
        [
            new(typeof(GrpcOnlyInterceptor), PipelinePosition.BeforeEndpoints, Transport: AuthKitTransport.Grpc)
        ];
    }

    public sealed class GrpcOnlyInterceptor : Grpc.Core.Interceptors.Interceptor
    {
    }

    [Fact]
    public void ConfigurePluginMiddlewares_InvalidPosition_Throws()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PluginApplicationConfiguration.ConfigurePluginMiddlewares(
                app, [], (PipelinePosition)999));
    }

    [Fact]
    public void ConfigurePluginMiddlewares_NullMiddlewareType_Throws()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        var plugin = new DeclarativePlugin(
            "null-plugin", [new PluginMiddleware(null!, PipelinePosition.BeforeEndpoints)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PluginApplicationConfiguration.ConfigurePluginMiddlewares(
                app, [Load(plugin)], PipelinePosition.BeforeEndpoints));
        Assert.Contains("null type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfigurePluginMiddlewares_UnsupportedEntryPosition_IsIgnored()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        var plugin = new DeclarativePlugin(
            "bad-position-plugin",
            [new PluginMiddleware(typeof(RecordedInterfaceMiddleware), (PipelinePosition)999)]);

        // Entry positions outside the enum can never match a validated request
        // position, so they are filtered out rather than registered.
        PluginApplicationConfiguration.ConfigurePluginMiddlewares(
            app, [Load(plugin)], PipelinePosition.BeforeEndpoints);

        Assert.NotNull(app.Build());
    }

    [Fact]
    public void ConfigurePluginMiddlewares_RegistrationFailure_NamesPlugin()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        var plugin = new DeclarativePlugin(
            "broken-plugin", [new PluginMiddleware(typeof(int), PipelinePosition.BeforeEndpoints)]);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PluginApplicationConfiguration.ConfigurePluginMiddlewares(
                app, [Load(plugin)], PipelinePosition.BeforeEndpoints));
        Assert.Contains("broken-plugin", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfigurePluginMiddlewares_ExecutesAllHttpModelsInOrder()
    {
        var markers = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(markers);
        var app = new ApplicationBuilder(services.BuildServiceProvider());
        var plugin = new DeclarativePlugin("http-plugin",
        [
            new PluginMiddleware(typeof(RecordedConventionMiddleware), PipelinePosition.BeforeEndpoints, Order: 2),
            new PluginMiddleware(typeof(RecordedInterfaceMiddleware), PipelinePosition.BeforeEndpoints, Order: 0),
            new PluginMiddleware(typeof(RecordedBaseMiddleware), PipelinePosition.BeforeEndpoints, Order: 1),
        ]);

        PluginApplicationConfiguration.ConfigurePluginMiddlewares(
            app, [Load(plugin)], PipelinePosition.BeforeEndpoints);

        var pipeline = app.Build();
        var context = new DefaultHttpContext
        {
            RequestServices = app.ApplicationServices,
        };
        await pipeline(context);

        Assert.Equal(["interface", "base", "convention"], markers);
    }

    [PluginMetadata("declarative-plugin", "1.0.0", [], [], [], description: "Declarative middleware test")]
    private sealed class DeclarativePlugin(string id, IReadOnlyList<PluginMiddleware> middlewares) : IAuthKitPlugin
    {
        public string Id => id;

        public IReadOnlyList<PluginMiddleware> Middlewares => middlewares;
    }

    public sealed class RecordedInterfaceMiddleware(List<string> markers)
        : AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            markers.Add("interface");
            return next(context);
        }
    }

    public sealed class RecordedBaseMiddleware(List<string> markers)
        : AuthKit.Plugins.Abstractions.Contracts.PluginContract.AuthKitMiddlewareBase
    {
        public override Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            markers.Add("base");
            return next(context);
        }
    }

    public sealed class RecordedConventionMiddleware(RequestDelegate next, List<string> markers)
    {
        private readonly RequestDelegate _next = next;

        public Task InvokeAsync(HttpContext context)
        {
            markers.Add("convention");
            return _next(context);
        }
    }
}