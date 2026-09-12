using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AuthKit.Host.IntegrationTests;

/// <summary>
/// Verifies plugin lifecycle hooks and plugin-owned hosted services against the
/// real .NET generic host lifecycle (not a mocked lifetime).
/// </summary>
public sealed class PluginLifecycleIntegrationTests
{
    [Fact]
    public async Task LifecycleHooks_And_HostedServices_FollowStandardHostLifecycle()
    {
        var events = new List<string>();
        var pluginA = new LifecycleRecordingPlugin(events, "plugin.a", "Plugin A");
        var pluginB = new LifecycleRecordingPlugin(events, "plugin.b", "Plugin B");
        var hosted = new RecordingHostedService(events);
        var hostedServicePlugin = new HostedServicePlugin(hosted);

        using var host = CreateHost([pluginA, pluginB, hostedServicePlugin]);

        await host.StartAsync();
        await host.StopAsync();

        Assert.Equal(
        [
            "starting:plugin.a",
            "starting:plugin.b",
            "hosted:start",
            "started:plugin.a",
            "started:plugin.b",
            "stopping:plugin.b",
            "stopping:plugin.a",
            "hosted:stop"
        ], events);
    }

    [Fact]
    public async Task LifecycleHooks_AreInvokedExactlyOnce()
    {
        var events = new List<string>();
        var plugin = new LifecycleRecordingPlugin(events, "plugin.a", "Plugin A");

        using var host = CreateHost([plugin]);

        await host.StartAsync();
        await host.StopAsync();

        Assert.Single(events, entry => entry == "starting:plugin.a");
        Assert.Single(events, entry => entry == "started:plugin.a");
        Assert.Single(events, entry => entry == "stopping:plugin.a");
    }

    [Fact]
    public async Task LifecycleHooks_ReceiveTheHostCancellationTokens()
    {
        var plugin = new LifecycleRecordingPlugin([], "plugin.a", "Plugin A");

        using var host = CreateHost([plugin]);

        await host.StartAsync();
        Assert.False(plugin.StartingToken.IsCancellationRequested);

        await host.StopAsync();
        Assert.True(plugin.StoppingToken.IsCancellationRequested);
    }

    [Fact]
    public void DuplicateHostedServiceRegistration_IsRejected()
    {
        var hosted = new RecordingHostedService([]);
        var plugin = new HostedServicePlugin(hosted);
        var loaded = Load(plugin);
        var services = new ServiceCollection();
        services.AddSingleton<IReadOnlyList<LoadedPlugin>>([loaded]);

        PluginHostedServiceRegistration.Register(services, [loaded]);

        Assert.Throws<InvalidOperationException>(() =>
            PluginHostedServiceRegistration.Register(services, [loaded]));
    }

    [Fact]
    public void SameHostedServiceReturnedTwice_IsRejected()
    {
        var hosted = new RecordingHostedService([]);
        var plugin = new DuplicateHostedServicePlugin(hosted);
        var services = new ServiceCollection();
        services.AddSingleton<IReadOnlyList<LoadedPlugin>>([Load(plugin)]);

        Assert.Throws<InvalidOperationException>(() =>
            PluginHostedServiceRegistration.Register(services, [Load(plugin)]));
    }

    [Fact]
    public void NullHostedServiceResult_IsRejected()
    {
        var plugin = new NullHostedServicePlugin();
        var services = new ServiceCollection();
        services.AddSingleton<IReadOnlyList<LoadedPlugin>>([Load(plugin)]);

        Assert.Throws<InvalidOperationException>(() =>
            PluginHostedServiceRegistration.Register(services, [Load(plugin)]));
    }

    private static IHost CreateHost(params IAuthKitPlugin[] plugins)
    {
        var loaded = plugins.Select(Load).ToArray();
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();

        builder.Services.AddSingleton<IReadOnlyList<LoadedPlugin>>(loaded);
        PluginHostedServiceRegistration.Register(builder.Services, loaded);

        return builder.Build();
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadata("plugin.a", "1.0.0", [], [], [], name: "Plugin A", description: "Lifecycle test")]
    private sealed class LifecycleRecordingPlugin(List<string> events, string id, string name) : IAuthKitPlugin
    {
        public string Id { get; } = id;
        public string Name { get; } = name;

        public CancellationToken StartingToken { get; private set; }
        public CancellationToken StartedToken { get; private set; }
        public CancellationToken StoppingToken { get; private set; }

        public Task OnStartingAsync(CancellationToken cancellationToken)
        {
            StartingToken = cancellationToken;
            events.Add($"starting:{Id}");
            return Task.CompletedTask;
        }

        public Task OnStartedAsync(CancellationToken cancellationToken)
        {
            StartedToken = cancellationToken;
            events.Add($"started:{Id}");
            return Task.CompletedTask;
        }

        public Task OnStoppingAsync(CancellationToken cancellationToken)
        {
            StoppingToken = cancellationToken;
            events.Add($"stopping:{Id}");
            return Task.CompletedTask;
        }
    }

    [PluginMetadata("plugin.hosted", "1.0.0", [], [], [], name: "Hosted Service Plugin", description: "Hosted service test")]
    private sealed class HostedServicePlugin(IHostedService hostedService) : IAuthKitPlugin
    {
        public IReadOnlyList<IHostedService> GetHostedServices() => [hostedService];
    }

    [PluginMetadata("plugin.duplicate", "1.0.0", [], [], [], name: "Duplicate Hosted Service Plugin", description: "Duplicate test")]
    private sealed class DuplicateHostedServicePlugin(IHostedService hostedService) : IAuthKitPlugin
    {
        public IReadOnlyList<IHostedService> GetHostedServices() => [hostedService, hostedService];
    }

    [PluginMetadata("plugin.null", "1.0.0", [], [], [], name: "Null Hosted Service Plugin", description: "Null test")]
    private sealed class NullHostedServicePlugin : IAuthKitPlugin
    {
        public IReadOnlyList<IHostedService> GetHostedServices() => null!;
    }

    private sealed class RecordingHostedService(List<string> events) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            events.Add("hosted:start");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            events.Add("hosted:stop");
            return Task.CompletedTask;
        }
    }
}