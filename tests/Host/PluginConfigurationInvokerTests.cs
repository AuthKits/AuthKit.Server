using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Plugins.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin configuration dispatch and context isolation.
/// </summary>
public sealed class PluginConfigurationInvokerTests
{
    [Fact]
    public void LegacyPlugin_UsesLegacyOverloadOnce()
    {
        var plugin = new LegacyPlugin();
        var builder = CreateBuilder();

        PluginConfigurationInvoker.Configure(plugin, builder, builder.Configuration);

        Assert.Equal(1, plugin.LegacyCalls);
    }

    [Fact]
    public void BuilderPlugin_ReceivesActualBuilderOnce()
    {
        var plugin = new BuilderPlugin();
        var builder = CreateBuilder();

        PluginConfigurationInvoker.Configure(plugin, builder, builder.Configuration);

        Assert.Same(builder.Services, plugin.Services);
        Assert.Equal(1, plugin.BuilderCalls);
        Assert.Equal(0, plugin.LegacyCalls);
    }

    [Fact]
    public void ContextPlugin_ReceivesScopedConfiguration()
    {
        var builder = CreateBuilder(
            ("Plugins:context-plugin:Value", "context-value"),
            ("Plugins:other-plugin:Value", "other-value"),
            ("HostValue", "host-value"));
        var plugin = new ContextPlugin();

        PluginConfigurationInvoker.Configure(plugin, builder, builder.Configuration);

        Assert.Equal(1, plugin.ContextCalls);
        Assert.Equal("context-plugin", plugin.Context!.PluginId);
        Assert.Equal("Context Plugin", plugin.Context.PluginName);
        Assert.Equal("context-value", plugin.Context.Configuration["Value"]);
        Assert.Null(plugin.Context.Configuration["HostValue"]);
        Assert.Equal("host-value", plugin.Context.ApplicationConfiguration["HostValue"]);
    }

    [Fact]
    public void ContextOverload_HasPriorityAndIsInvokedOnlyOnce()
    {
        var builder = CreateBuilder(("Plugins:both-plugin:Value", "context-value"));
        var plugin = new ContextAndBuilderPlugin();

        PluginConfigurationInvoker.Configure(plugin, builder, builder.Configuration);

        Assert.Equal(1, plugin.ContextCalls);
        Assert.Equal(0, plugin.BuilderCalls);
        Assert.Equal(0, plugin.LegacyCalls);
    }

    [Fact]
    public void MultiplePlugins_ReceiveIndependentContexts()
    {
        var builder = CreateBuilder(
            ("Plugins:context-plugin:Value", "first"),
            ("Plugins:other-plugin:Value", "second"));
        var first = new ContextPlugin();
        var second = new OtherContextPlugin();

        PluginConfigurationInvoker.Configure(first, builder, builder.Configuration);
        PluginConfigurationInvoker.Configure(second, builder, builder.Configuration);

        Assert.Equal("first", first.Context!.Configuration["Value"]);
        Assert.Equal("second", second.Context!.Configuration["Value"]);
        Assert.NotSame(first.Context.Configuration, second.Context.Configuration);
    }

    private static HostApplicationBuilder CreateBuilder(params (string Key, string Value)[] values)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(value =>
                new KeyValuePair<string, string?>(value.Key, value.Value)))
            .Build();

        builder.Configuration.AddConfiguration(configuration);
        return builder;
    }

    [PluginMetadata("legacy-plugin", "1.0.0", [], [], [], name: "Legacy Plugin", description: "Test plugin")]
    private sealed class LegacyPlugin : IAuthKitPlugin
    {
        public int LegacyCalls { get; private set; }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration) => LegacyCalls++;
    }

    [PluginMetadata("builder-plugin", "1.0.0", [], [], [], name: "Builder Plugin", description: "Test plugin")]
    private sealed class BuilderPlugin : IAuthKitPlugin
    {
        public int LegacyCalls { get; private set; }
        public int BuilderCalls { get; private set; }
        public IServiceCollection? Services { get; private set; }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration) => LegacyCalls++;

        public void ConfigureServices(IHostApplicationBuilder builder, IConfiguration configuration)
        {
            BuilderCalls++;
            Services = builder.Services;
        }

    }

    [PluginMetadata("context-plugin", "1.0.0", [], [], [], name: "Context Plugin", description: "Test plugin")]
    private sealed class ContextPlugin : IAuthKitPlugin
    {
        public int ContextCalls { get; private set; }
        public AuthKitPluginContext? Context { get; private set; }

        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context)
        {
            ContextCalls++;
            Context = context;
        }
    }

    [PluginMetadata("both-plugin", "1.0.0", [], [], [], name: "Both Plugin", description: "Test plugin")]
    private sealed class ContextAndBuilderPlugin : IAuthKitPlugin
    {
        public int LegacyCalls { get; private set; }
        public int BuilderCalls { get; private set; }
        public int ContextCalls { get; private set; }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration) => LegacyCalls++;
        public void ConfigureServices(IHostApplicationBuilder builder, IConfiguration configuration) => BuilderCalls++;
        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) => ContextCalls++;
    }

    [PluginMetadata("other-plugin", "1.0.0", [], [], [], name: "Other Plugin", description: "Test plugin")]
    private sealed class OtherContextPlugin : IAuthKitPlugin
    {
        public AuthKitPluginContext? Context { get; private set; }

        public void ConfigureServices(IServiceCollection services, AuthKitPluginContext context) => Context = context;
    }
}