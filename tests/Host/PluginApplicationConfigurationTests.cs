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

    [Fact]
    public void ConfigureApplication_TakesPrecedenceOverLegacyMiddlewareType()
    {
        var app = WebApplication.CreateBuilder().Build();

        PluginApplicationConfiguration.ConfigureLegacyMiddleware(
            app, [Load(new ApplicationAndLegacyMiddlewarePlugin())]);
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

    [PluginMetadata("application-and-legacy-plugin", "1.0.0", [], [], [], description: "Duplicate registration test")]
    private sealed class ApplicationAndLegacyMiddlewarePlugin : IAuthKitPlugin
    {
        public Type MiddlewareType => typeof(string);

        public void ConfigureApplication(IApplicationBuilder application)
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
}