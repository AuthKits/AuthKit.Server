using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using Host.Plugins;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies plugin lifecycle ordering, cancellation and hosted service registration.
/// </summary>
public sealed class PluginLifecycleHostedServiceTests
{
    [Fact]
    public void LifecycleHooks_AreOrderedAndStoppingRunsInReverse()
    {
        var events = new List<string>();
        var lifetime = new TestLifetime();
        var pluginA = new LifecyclePlugin("plugin.a", events);
        var pluginB = new LifecyclePlugin("plugin.b", events);
        var service = new PluginLifecycleHostedService(
            [Load(pluginB), Load(pluginA)], lifetime, NullLogger<PluginLifecycleHostedService>.Instance);

        service.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        lifetime.Started.Cancel();
        lifetime.Stopping.Cancel();

        Assert.Equal(
            ["plugin.a:starting", "plugin.b:starting", "plugin.a:started", "plugin.b:started",
             "plugin.b:stopping", "plugin.a:stopping"],
            events);
    }

    [Fact]
    public void LifecycleHooks_ReceiveHostCancellationTokens()
    {
        var lifetime = new TestLifetime();
        var plugin = new LifecyclePlugin("plugin", []);
        var service = new PluginLifecycleHostedService(
            [Load(plugin)], lifetime, NullLogger<PluginLifecycleHostedService>.Instance);
        using var startupCancellation = new CancellationTokenSource();

        service.StartAsync(startupCancellation.Token).GetAwaiter().GetResult();
        startupCancellation.Cancel();
        lifetime.Started.Cancel();
        lifetime.Stopping.Cancel();

        Assert.True(plugin.StartingToken.IsCancellationRequested);
        Assert.True(plugin.StartedToken.IsCancellationRequested);
        Assert.True(plugin.StoppingToken.IsCancellationRequested);
    }

    [Fact]
    public void HostedServices_AreRegisteredInStandardDi()
    {
        var hostedService = new RecordingHostedService();
        var plugin = new HostedServicePlugin(hostedService);
        var services = new ServiceCollection();
        services.AddSingleton<IReadOnlyList<LoadedPlugin>>([Load(plugin)]);
        services.AddSingleton<IHostApplicationLifetime>(new TestLifetime());
        services.AddSingleton<ILogger<PluginLifecycleHostedService>>(
            NullLogger<PluginLifecycleHostedService>.Instance);

        PluginHostedServiceRegistration.Register(services, [Load(plugin)]);

        var provider = services.BuildServiceProvider();
        Assert.Contains(hostedService, provider.GetServices<IHostedService>());
    }

    [Fact]
    public void LifecycleFailure_IsSurfacedWithPluginAndStage()
    {
        var plugin = new FailingLifecyclePlugin();
        var service = new PluginLifecycleHostedService(
            [Load(plugin)], new TestLifetime(), NullLogger<PluginLifecycleHostedService>.Instance);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None).GetAwaiter().GetResult());

        Assert.Contains("failing-plugin", exception.Message);
        Assert.Contains("OnStarting", exception.Message);
    }

    [PluginMetadata("lifecycle-plugin", "1.0.0", [], [], [], description: "Lifecycle test")]
    private sealed class LifecyclePlugin(string id, List<string> events) : IAuthKitPlugin
    {
        public string Id { get; } = id;
        public CancellationToken StartingToken { get; private set; }
        public CancellationToken StartedToken { get; private set; }
        public CancellationToken StoppingToken { get; private set; }

        public Task OnStartingAsync(CancellationToken cancellationToken)
        {
            StartingToken = cancellationToken;
            events.Add($"{Id}:starting");
            return Task.CompletedTask;
        }

        public Task OnStartedAsync(CancellationToken cancellationToken)
        {
            StartedToken = cancellationToken;
            events.Add($"{Id}:started");
            return Task.CompletedTask;
        }

        public Task OnStoppingAsync(CancellationToken cancellationToken)
        {
            StoppingToken = cancellationToken;
            events.Add($"{Id}:stopping");
            return Task.CompletedTask;
        }
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadata("hosted-service-plugin", "1.0.0", [], [], [], description: "Hosted service test")]
    private sealed class HostedServicePlugin(IHostedService hostedService) : IAuthKitPlugin
    {
        public IReadOnlyList<IHostedService> GetHostedServices() => [hostedService];
    }

    private sealed class RecordingHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [PluginMetadata("failing-plugin", "1.0.0", [], [], [], description: "Failure test")]
    private sealed class FailingLifecyclePlugin : IAuthKitPlugin
    {
        public Task OnStartingAsync(CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("startup failure"));
    }

    private sealed class TestLifetime : IHostApplicationLifetime
    {
        public CancellationTokenSource Started { get; } = new();
        public CancellationTokenSource Stopping { get; } = new();

        public CancellationToken ApplicationStarted => Started.Token;
        public CancellationToken ApplicationStopping => Stopping.Token;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => Stopping.Cancel();
    }
}