using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;
using Host.Plugins.Configuration;
using Host.Plugins.Loading;
using Xunit;

namespace AuthKit.Host.Tests;

public sealed class PluginGrpcConfigurationTests
{
    [Fact]
    public void GrpcEntries_AreOrderedByPositionThenOrderThenPluginIdThenIndex()
    {
        var plugins = new List<LoadedPlugin>
        {
            Load("b-plugin",
                new(typeof(FakeInterceptor), PipelinePosition.BeforeEndpoints, Order: 5, Transport: AuthKitTransport.Grpc),
                new(typeof(FakeInterceptor), PipelinePosition.BeforeRouting, Order: 5, Transport: AuthKitTransport.Grpc)),
            Load("a-plugin",
                new(typeof(FakeInterceptor), PipelinePosition.BeforeEndpoints, Order: 5, Transport: AuthKitTransport.Grpc),
                new(typeof(FakeInterceptor), PipelinePosition.BeforeEndpoints, Order: 1, Transport: AuthKitTransport.Grpc)),
        };

        var ordered = PluginGrpcConfiguration.OrderGrpcInterceptors(plugins);

        Assert.Equal(4, ordered.Count);
        // Semantic position first.
        Assert.Equal(PipelinePosition.BeforeRouting, ordered[0].Entry.Position);
        // Then Order within BeforeEndpoints.
        Assert.Equal("a-plugin", ordered[1].PluginId);
        Assert.Equal(1, ordered[1].Entry.Order);
        // Same position+order: stable PluginId, then declaration index.
        Assert.Equal("a-plugin", ordered[2].PluginId);
        Assert.Equal("b-plugin", ordered[3].PluginId);
    }

    [Fact]
    public void HttpEntriesAndDisabledEntries_AreExcluded()
    {
        var plugins = new List<LoadedPlugin>
        {
            Load("http-plugin",
                new(typeof(FakeInterceptor), PipelinePosition.BeforeRouting, Transport: AuthKitTransport.Http),
                new(typeof(FakeInterceptor), PipelinePosition.BeforeRouting, IsMiddlewareEnabled: false, Transport: AuthKitTransport.Grpc)),
        };

        var ordered = PluginGrpcConfiguration.OrderGrpcInterceptors(plugins);

        Assert.Empty(ordered);
    }

    private static LoadedPlugin Load(string id, params PluginMiddleware[] middlewares) =>
        new(new FakePlugin(id, middlewares), typeof(PluginGrpcConfigurationTests).Assembly, "/test");

    private sealed class FakePlugin(string id, IReadOnlyList<PluginMiddleware> middlewares) : IAuthKitPlugin
    {
        public string Id => id;

        public string Name => id;

        public IReadOnlyList<PluginMiddleware> Middlewares => middlewares;
    }

    public sealed class FakeInterceptor : Grpc.Core.Interceptors.Interceptor
    {
    }
}
