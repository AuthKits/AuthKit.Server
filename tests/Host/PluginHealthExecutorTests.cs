using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.Plugins;
using AuthKit.Plugins.Abstractions.Models;
using Host.Plugins.Loading;
using Host.Plugins.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using IAuthKitPlugin = AuthKit.Plugins.Abstractions.Contracts.PluginContract.IAuthKitPlugin;

namespace AuthKit.Host.Tests;

/// <summary>
/// Verifies scoped plugin health execution, host timing, and cache behavior.
/// </summary>
public sealed class PluginHealthExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_UsesAndDisposesDedicatedScope()
    {
        var probeInstances = new List<ScopedProbe>();
        var services = CreateServices(TimeSpan.Zero, probeInstances);
        await using var provider = services.BuildServiceProvider();
        var plugin = new TrackingPlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var execution = await executor.ExecuteAsync(Load(plugin));

        Assert.Single(probeInstances);
        Assert.True(probeInstances[0].Disposed);
        Assert.Equal(1, plugin.Calls);
        Assert.Equal(PluginHealthStatus.Healthy, execution.Results.Single().Status);
        Assert.True(execution.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_CachesCompletedExecutionWithinTtl()
    {
        var services = CreateServices(TimeSpan.FromMinutes(1), []);
        await using var provider = services.BuildServiceProvider();
        var plugin = new TrackingPlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var first = await executor.ExecuteAsync(Load(plugin));
        var second = await executor.ExecuteAsync(Load(plugin));

        Assert.Equal(1, plugin.Calls);
        Assert.Equal(first.Duration, second.Duration);
        Assert.Equal(first.Results, second.Results);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledCache_ExecutesEveryRequest()
    {
        var services = CreateServices(TimeSpan.Zero, []);
        await using var provider = services.BuildServiceProvider();
        var plugin = new TrackingPlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        await executor.ExecuteAsync(Load(plugin));
        await executor.ExecuteAsync(Load(plugin));

        Assert.Equal(2, plugin.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_RefreshesAfterCacheTtlExpires()
    {
        var services = CreateServices(TimeSpan.FromMilliseconds(1), []);
        await using var provider = services.BuildServiceProvider();
        var plugin = new TrackingPlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        await executor.ExecuteAsync(Load(plugin));
        await Task.Delay(25);
        await executor.ExecuteAsync(Load(plugin));

        Assert.Equal(2, plugin.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_DeduplicatesConcurrentRefreshes()
    {
        var services = CreateServices(TimeSpan.FromMinutes(1), []);
        await using var provider = services.BuildServiceProvider();
        var plugin = new DelayedPlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        await Task.WhenAll(
            executor.ExecuteAsync(Load(plugin)),
            executor.ExecuteAsync(Load(plugin)));

        Assert.Equal(1, plugin.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesFailureAndDoesNotCacheIt()
    {
        var services = CreateServices(TimeSpan.FromMinutes(1), []);
        await using var provider = services.BuildServiceProvider();
        var plugin = new FailingOncePlugin();
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(Load(plugin)));

        Assert.Equal("health failure", exception.Message);

        var execution = await executor.ExecuteAsync(Load(plugin));

        Assert.Equal(2, plugin.Calls);
        Assert.Equal(PluginHealthStatus.Healthy, execution.Results.Single().Status);
    }

    [Fact]
    public async Task ExecuteAsync_IsolatesCacheEntriesBetweenPlugins()
    {
        var services = CreateServices(TimeSpan.FromMinutes(1), []);
        await using var provider = services.BuildServiceProvider();
        var first = new IsolatedPlugin("plugin.first");
        var second = new IsolatedPlugin("plugin.second");
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var executions = await Task.WhenAll(
            executor.ExecuteAsync(Load(first)),
            executor.ExecuteAsync(Load(second)));

        await executor.ExecuteAsync(Load(first));
        await executor.ExecuteAsync(Load(second));

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal("shared", executions[0].Results.Single().Reason);
        Assert.Equal("shared", executions[1].Results.Single().Reason);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsPositiveHostMeasuredDuration()
    {
        var services = CreateServices(TimeSpan.Zero, []);
        await using var provider = services.BuildServiceProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var plugin = new TimedPlugin(started, release);
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var executionTask = executor.ExecuteAsync(Load(plugin));
        await started.Task;
        await Task.Delay(20);
        release.SetResult();

        var execution = await executionTask;

        Assert.True(execution.Duration >= TimeSpan.FromMilliseconds(15));
        Assert.Null(execution.Results.Single().Data?["latency_ms"]);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationDoesNotPopulateCache()
    {
        var services = CreateServices(TimeSpan.FromMinutes(1), []);
        await using var provider = services.BuildServiceProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var plugin = new CancellingPlugin(started);
        var executor = provider.GetRequiredService<PluginHealthExecutor>();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var cancelledExecution = executor.ExecuteAsync(Load(plugin), cancellation.Token);
        await started.Task;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledExecution);

        await executor.ExecuteAsync(Load(plugin));

        Assert.Equal(2, plugin.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_DifferentPluginsExecuteConcurrently()
    {
        var services = CreateServices(TimeSpan.Zero, []);
        await using var provider = services.BuildServiceProvider();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var alpha = new AlphaBarrierPlugin(barrier);
        var beta = new BetaBarrierPlugin(barrier);
        var executor = provider.GetRequiredService<PluginHealthExecutor>();

        var alphaExecution = executor.ExecuteAsync(Load(alpha));
        var betaExecution = executor.ExecuteAsync(Load(beta));

        await Task.Delay(100);
        Assert.Equal(2, alpha.Calls + beta.Calls);

        barrier.SetResult();
        await Task.WhenAll(alphaExecution, betaExecution);
    }

    private static ServiceCollection CreateServices(
        TimeSpan cacheTtl,
        List<ScopedProbe> probeInstances)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            var probe = new ScopedProbe();
            probeInstances.Add(probe);
            return probe;
        });
        services.Configure<PluginHealthExecutionOptions>(options => options.CacheTtl = cacheTtl);
        services.AddSingleton<PluginHealthExecutor>();
        return services;
    }

    private static LoadedPlugin Load(IAuthKitPlugin plugin) =>
        new(plugin, plugin.GetType().Assembly, "test");

    [PluginMetadata("tracking-health", "1.0.0", [], [], [], description: "Tracking health test")]
    private sealed class TrackingPlugin : IAuthKitPlugin
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            _ = services.GetRequiredService<ScopedProbe>();
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Healthy)]);
        }
    }

    [PluginMetadata("cancelling-health", "1.0.0", [], [], [], description: "Cancellation health test")]
    private sealed class CancellingPlugin(TaskCompletionSource started) : IAuthKitPlugin
    {
        public int Calls { get; private set; }

        public async Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            started.TrySetResult();
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return [new PluginHealthResult(PluginHealthStatus.Healthy)];
        }
    }

    [PluginMetadata("delayed-health", "1.0.0", [], [], [], description: "Concurrent health test")]
    private sealed class DelayedPlugin : IAuthKitPlugin
    {
        public int Calls { get; private set; }

        public async Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            await Task.Delay(25, cancellationToken);
            return [new(PluginHealthStatus.Healthy)];
        }
    }

    [PluginMetadata("failing-once-health", "1.0.0", [], [], [], description: "Failure health test")]
    private sealed class FailingOncePlugin : IAuthKitPlugin
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls == 1)
                return Task.FromException<IReadOnlyList<PluginHealthResult>>(
                    new InvalidOperationException("health failure"));

            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Healthy)]);
        }
    }

    [PluginMetadata("plugin.first", "1.0.0", [], [], [], description: "First isolated health test")]
    private sealed class IsolatedPlugin(string id) : IAuthKitPlugin
    {
        public int Calls { get; private set; }
        public string Id { get; } = id;

        public Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<PluginHealthResult>>(
                [new(PluginHealthStatus.Healthy, "shared")]);
        }
    }

    [PluginMetadata("timed-health", "1.0.0", [], [], [], description: "Timed health test")]
    private sealed class TimedPlugin(
        TaskCompletionSource started,
        TaskCompletionSource release) : IAuthKitPlugin
    {
        public async Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return [new(PluginHealthStatus.Healthy)];
        }
    }

    [PluginMetadata("barrier-alpha-health", "1.0.0", [], [], [], description: "Barrier alpha health test")]
    private sealed class AlphaBarrierPlugin(TaskCompletionSource barrier) : BarrierPlugin(barrier)
    {
    }

    [PluginMetadata("barrier-beta-health", "1.0.0", [], [], [], description: "Barrier beta health test")]
    private sealed class BetaBarrierPlugin(TaskCompletionSource barrier) : BarrierPlugin(barrier)
    {
    }

    private abstract class BarrierPlugin(TaskCompletionSource barrier) : IAuthKitPlugin
    {
        public int Calls { get; private set; }

        public async Task<IReadOnlyList<PluginHealthResult>> CheckHealthAsync(
            IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            await barrier.Task.WaitAsync(cancellationToken);
            return [new PluginHealthResult(PluginHealthStatus.Healthy)];
        }
    }

    private sealed class ScopedProbe : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}